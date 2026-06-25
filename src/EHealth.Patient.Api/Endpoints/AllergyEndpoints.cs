using EHealth.PatientApi.Data;
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

            var ual = await PublishToDkg(allergy, http, config);
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

    private static async Task<string?> PublishToDkg(
        AllergyRecord allergy, IHttpClientFactory http, IConfiguration config)
    {
        try
        {
            var mfssiaUrl = config["MfssiaUrl"] ?? "http://mfssia-ehealth:4000/api";
            var client = http.CreateClient();

            // JSON-LD so the allergy is queryable in the DKG graph (raw Turtle is not parsed).
            var response = await client.PostAsJsonAsync($"{mfssiaUrl}/rdf/jsonld", new
            {
                id = $"urn:patient:allergy:{allergy.Id}",
                type = "Allergy",
                literals = new Dictionary<string, string>
                {
                    ["patientId"] = allergy.PatientId.ToString(),
                    ["substance"] = allergy.Substance,
                    ["snomedCode"] = allergy.SnomedCode,
                    ["source"] = allergy.Source,
                },
                dateTimes = new Dictionary<string, string>
                {
                    ["recordedAt"] = allergy.RecordedAt.ToUniversalTime().ToString("O"),
                },
            });

            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadFromJsonAsync<DkgResponse>();
            return json?.Data?.UAL ?? json?.UAL;
        }
        catch { return null; }
    }

    private record DkgData(string? UAL);
    private record DkgResponse(string? UAL, DkgData? Data);
}
