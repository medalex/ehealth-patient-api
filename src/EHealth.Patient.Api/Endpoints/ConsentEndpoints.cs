using EHealth.PatientApi.Data;
using EHealth.PatientApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EHealth.PatientApi.Endpoints;

public static class ConsentEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/consents").WithTags("Consents");

        group.MapGet("/patient/{patientId:guid}", async (Guid patientId, AppDbContext db) =>
            await db.DataConsents
                .Where(c => c.PatientId == patientId)
                .ToListAsync());

        group.MapPost("/", async (DataConsent consent, AppDbContext db) =>
        {
            consent.Id = Guid.NewGuid();
            consent.GrantedAt = DateTime.UtcNow;
            db.DataConsents.Add(consent);
            await db.SaveChangesAsync();
            return Results.Created($"/api/consents/{consent.Id}", consent);
        });

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var consent = await db.DataConsents.FindAsync(id);
            if (consent is null) return Results.NotFound();
            db.DataConsents.Remove(consent);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
