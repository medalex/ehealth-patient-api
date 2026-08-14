using EHealth.PatientApi.Data;
using EHealth.PatientApi.Dkg;
using EHealth.PatientApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EHealth.PatientApi.Endpoints;

public static class AllergyEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/allergies").WithTags("Allergies");

        // orgId query param: if provided, consent check is enforced (used by hospital/lab)
        group.MapGet("/patient/{patientId:guid}", async (
            Guid patientId, string? orgId, AppDbContext db) =>
        {
            if (orgId is not null)
            {
                var hasConsent = await db.DataConsents.AnyAsync(c =>
                    c.PatientId == patientId &&
                    c.OrganizationId == orgId &&
                    (c.ExpiresAt == null || c.ExpiresAt > DateTime.UtcNow));

                if (!hasConsent)
                    return Results.StatusCode(403);
            }

            var records = await db.AllergyRecords
                .Where(a => a.PatientId == patientId)
                .ToListAsync();

            return Results.Ok(records);
        });

        group.MapPost("/", async (AllergyRecord allergy, AppDbContext db,
            IHttpClientFactory http, IConfiguration config) =>
        {
            allergy.Id = Guid.NewGuid();
            allergy.RecordedAt = DateTime.UtcNow;

            db.AllergyRecords.Add(allergy);
            await db.SaveChangesAsync();

            // Anchor the allergy in DKG (rx:Allergy) so MFSSIA can resolve it via SPARQL —
            // same publisher the seeding path in Program.cs uses.
            var ual = await AllergyDkg.PublishAsync(allergy, http, config);
            if (ual is not null)
            {
                allergy.DkgUal = ual;
                await db.SaveChangesAsync();
            }

            return Results.Created($"/api/allergies/{allergy.Id}", allergy);
        });

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var allergy = await db.AllergyRecords.FindAsync(id);
            if (allergy is null) return Results.NotFound();
            db.AllergyRecords.Remove(allergy);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
