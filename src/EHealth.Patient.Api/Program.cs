using EHealth.PatientApi.Data;
using EHealth.PatientApi.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite("Data Source=data.db"));

builder.Services.AddHttpClient();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
    c.SwaggerDoc("v1", new() { Title = "eHealth Patient Service", Version = "v1" }));

builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    // On a fresh seed, anchor seeded records in DKG so MFSSIA can resolve them via SPARQL:
    //   - consents  → physician-access gate (C-DOC-AUTHZ)
    //   - allergies → patient-record / contraindication proof (ZKP P2 NoContraindication)
    if (Seeder.Seed(db))
    {
        var http = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        foreach (var consent in db.DataConsents.ToList())
        {
            var ual = await EHealth.PatientApi.Dkg.ConsentDkg.PublishAsync(consent, http, config);
            if (ual is not null) consent.DkgUal = ual;
        }

        foreach (var allergy in db.AllergyRecords.ToList())
        {
            var ual = await EHealth.PatientApi.Dkg.AllergyDkg.PublishAsync(allergy, http, config);
            if (ual is not null) allergy.DkgUal = ual;
        }

        await db.SaveChangesAsync();
    }
}

app.UseCors();
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Patient API v1"));
PatientEndpoints.Map(app);
AllergyEndpoints.Map(app);
ConsentEndpoints.Map(app);

app.Run();

public partial class Program { }
