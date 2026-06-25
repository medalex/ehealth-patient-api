using System.Net.Http.Json;
using EHealth.PatientApi.Models;

namespace EHealth.PatientApi.Dkg;

// Anchors DataSharingConsent commitments in DKG via mfssia-ehealth.
// Predicates (rx:patient, rx:consentCovers, rx:validUntil) match the
// C-DOC-AUTHZ oracle SPARQL so physician access checks can resolve consent.
public static class ConsentDkg
{
    public static async Task<string?> PublishAsync(
        DataConsent consent, IHttpClientFactory http, IConfiguration config)
    {
        try
        {
            var mfssiaUrl = config["MfssiaUrl"] ?? "http://mfssia-ehealth:4000/api";
            var client = http.CreateClient();

            // Never-expiring consent is anchored with a far-future validUntil so the
            // oracle FILTER(?t > NOW()) still matches.
            var validUntil = consent.ExpiresAt ?? DateTime.UtcNow.AddYears(100);

            var turtle = $"""
                @prefix rx: <https://mfssia.io/ontology/prescription#> .
                @prefix xsd: <http://www.w3.org/2001/XMLSchema#> .

                <urn:consent:{consent.Id}> a rx:DataSharingConsent ;
                    rx:patient "{consent.PatientId}" ;
                    rx:consentCovers "{consent.OrganizationId}" ;
                    rx:grantedAt "{consent.GrantedAt:O}"^^xsd:dateTime ;
                    rx:validUntil "{validUntil:O}"^^xsd:dateTime .
                """;

            var response = await client.PostAsync(
                $"{mfssiaUrl}/rdf",
                new StringContent(turtle, System.Text.Encoding.UTF8, "text/turtle"));

            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadFromJsonAsync<DkgResponse>();
            return json?.Data?.UAL ?? json?.UAL;
        }
        catch { return null; }
    }

    // Anchors a ConsentRevocation tombstone in DKG. DKG assets are immutable and
    // cannot be deleted, so revocation is an append-only assertion that references
    // the original consent. The physician-access gate excludes revoked consents.
    public static async Task<string?> RevokeAsync(
        Guid consentId, IHttpClientFactory http, IConfiguration config)
    {
        try
        {
            var mfssiaUrl = config["MfssiaUrl"] ?? "http://mfssia-ehealth:4000/api";
            var client = http.CreateClient();

            var turtle = $"""
                @prefix rx: <https://mfssia.io/ontology/prescription#> .
                @prefix xsd: <http://www.w3.org/2001/XMLSchema#> .

                <urn:consent-revocation:{consentId}> a rx:ConsentRevocation ;
                    rx:revokes <urn:consent:{consentId}> ;
                    rx:revokedAt "{DateTime.UtcNow:O}"^^xsd:dateTime .
                """;

            var response = await client.PostAsync(
                $"{mfssiaUrl}/rdf",
                new StringContent(turtle, System.Text.Encoding.UTF8, "text/turtle"));

            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadFromJsonAsync<DkgResponse>();
            return json?.Data?.UAL ?? json?.UAL;
        }
        catch { return null; }
    }

    private record DkgData(string? UAL);
    private record DkgResponse(string? UAL, DkgData? Data);
}
