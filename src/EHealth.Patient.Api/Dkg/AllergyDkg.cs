using System.Net.Http.Json;
using EHealth.PatientApi.Models;

namespace EHealth.PatientApi.Dkg;

// Publishes a patient allergy to the DKG as an rx:Allergy Knowledge Asset so that
// MFSSIA's patient-record / contraindication modules can resolve it via SPARQL
// (the ZKP contraindication check P2 binds to this data).
public static class AllergyDkg
{
    public static async Task<string?> PublishAsync(
        AllergyRecord allergy, 
        IHttpClientFactory http, 
        IConfiguration config)
    {
        try
        {
            var mfssiaUrl = Mfssia.BaseUrl(config);
            var client = http.CreateClient();

            // JSON-LD aligned to the rx: ontology TBox (rx:Allergy: hasPatient/hasSubstance
            // as IRIs, snomedCode literal, hasSource/hasTimestamp).
            var sourceSlug = allergy.Source.Trim().Replace(" ", "-").ToLowerInvariant();
            var response = await client.PostAsJsonAsync(
                $"{mfssiaUrl}/rdf/jsonld", 
                new
                {
                    id = $"urn:patient:allergy:{allergy.Id}",
                    type = "Allergy",
                    iris = new Dictionary<string, string>
                    {
                        ["hasPatient"] = $"urn:patient:{allergy.PatientId}",
                        ["hasSubstance"] = $"rx:{allergy.Substance}",
                        ["hasSource"] = $"urn:org:{sourceSlug}",
                    },
                    literals = new Dictionary<string, string>
                    {
                        ["snomedCode"] = allergy.SnomedCode,
                    },
                    dateTimes = new Dictionary<string, string>
                    {
                        ["hasTimestamp"] = allergy.RecordedAt.ToUniversalTime().ToString("O"),
                    },
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
}
