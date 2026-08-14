using EHealth.PatientApi.Data;
using EHealth.PatientApi.Dkg;
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

        // Check for an active consent record: 200 OK / 403 Forbidden
        group.MapGet("/check", async (
            Guid patientId, 
            string organizationId, 
            AppDbContext db) =>
        {
            var now = DateTime.UtcNow;
            var granted = await db.DataConsents.AnyAsync(c =>
                c.PatientId == patientId &&
                c.OrganizationId == organizationId &&
                (c.ExpiresAt == null || c.ExpiresAt > now));
            return granted
                ? Results.Ok(new { granted = true })
                : Results.Json(new { granted = false, error = $"No active consent from patient {patientId} for organization {organizationId}" },
                    statusCode: 403);
        });

        group.MapPost("/", async (
            DataConsent consent, 
            AppDbContext db,
            IHttpClientFactory http, 
            IConfiguration config) =>
        {
            if (consent.PatientId == Guid.Empty) {
                return Results.BadRequest(new { error = "PatientId is required" });
            }

            if (string.IsNullOrWhiteSpace(consent.OrganizationId)) {
                return Results.BadRequest(new { error = "OrganizationId is required" });
            }

            consent.Id = Guid.NewGuid();
            consent.GrantedAt = DateTime.UtcNow;

            db.DataConsents.Add(consent);

            await db.SaveChangesAsync();

            // Anchor the DataSharingConsent commitment in DKG (rx:consentCovers organization)
            var ual = await ConsentDkg.PublishAsync(consent, http, config);
            
            if (ual is not null)
            {
                consent.DkgUal = ual;
                await db.SaveChangesAsync();
            }

            return Results.Created($"/api/consents/{consent.Id}", consent);
        });

        group.MapDelete("/{id:guid}", async (
            Guid id, 
            AppDbContext db,
            IHttpClientFactory http, 
            IConfiguration config) =>
        {
            var consent = await db.DataConsents.FindAsync(id);

            if (consent is null) {
                return Results.NotFound();
            }

            // Anchor a revocation tombstone in DKG (assets are immutable — cannot be
            // deleted). The physician-access gate then excludes this consent.
            await ConsentDkg.RevokeAsync(id, http, config);

            db.DataConsents.Remove(consent);

            await db.SaveChangesAsync();

            return Results.NoContent();
        });
    }
}
