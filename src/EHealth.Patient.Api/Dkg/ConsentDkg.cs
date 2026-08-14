using System.Net.Http.Json;
using EHealth.PatientApi.Models;

namespace EHealth.PatientApi.Dkg;

// Anchors DataSharingConsent commitments in DKG via mfssia-ehealth.
// Uses the JSON-LD consent endpoints (not raw Turtle /rdf): the DKG node stores
// raw Turtle but does NOT parse it into queryable triples, so the C-DOC-AUTHZ
// gate SPARQL would never find a Turtle-published consent. JSON-LD via createAsset
// produces proper triples (rx:patient, rx:consentCovers, rx:validUntil).
public static class ConsentDkg
{
    public static async Task<string?> PublishAsync(
        DataConsent consent, 
        IHttpClientFactory http, 
        IConfiguration config)
    {
        try
        {
            var mfssiaUrl = Mfssia.BaseUrl(config);
            var client = http.CreateClient();

            // Never-expiring consent is anchored with a far-future validUntil so the
            // gate FILTER(?t > NOW()) still matches.
            var validUntil = consent.ExpiresAt ?? DateTime.UtcNow.AddYears(100);

            var response = await client.PostAsJsonAsync(
                $"{mfssiaUrl}/consents/publish", 
                new
                {
                    consentId = consent.Id.ToString(),
                    patientId = consent.PatientId.ToString(),
                    organizationId = consent.OrganizationId,
                    grantedAt = consent.GrantedAt.ToUniversalTime().ToString("O"),
                    validUntil = validUntil.ToUniversalTime().ToString("O"),
                }
            );

            if (!response.IsSuccessStatusCode) {
                return null;
            }

            var json = await response.Content.ReadFromJsonAsync<DkgResponse>();
            
            return json?.Data?.UAL ?? json?.UAL;
        }
        catch { 
            return null; 
        }
    }

    // Anchors a ConsentRevocation tombstone in DKG. DKG assets are immutable and
    // cannot be deleted, so revocation is an append-only assertion that references
    // the original consent. The physician-access gate excludes revoked consents.
    public static async Task<string?> RevokeAsync(
        Guid consentId, 
        IHttpClientFactory http, 
        IConfiguration config)
    {
        try
        {
            var mfssiaUrl = Mfssia.BaseUrl(config);
            var client = http.CreateClient();

            var response = await client.PostAsJsonAsync(
                $"{mfssiaUrl}/consents/revoke", 
                new { consentId = consentId.ToString() }
            );

            if (!response.IsSuccessStatusCode) {
                return null;
            }

            var json = await response.Content.ReadFromJsonAsync<DkgResponse>();

            return json?.Data?.UAL ?? json?.UAL;
        }
        catch { 
            return null; 
        }
    }
}
