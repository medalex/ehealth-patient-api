using System.Net;
using System.Net.Http.Json;
using EHealth.PatientApi.Models;
using EHealth.PatientApi.Tests.Helpers;

namespace EHealth.PatientApi.Tests;

public class AllergyEndpointsTests : IDisposable
{
    private readonly TestFactory _factory = new();
    private readonly HttpClient _client;
    private static readonly Guid PatientId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public AllergyEndpointsTests()
    {
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAllergies_NoOrg_ReturnsRecords()
    {
        // No allergies are seeded — add one, then read it back.
        await _client.PostAsJsonAsync("/api/allergies", new AllergyRecord
        {
            PatientId = PatientId, Substance = "Penicillin", SnomedCode = "372687004", Source = "Patient PHR"
        });

        var response = await _client.GetAsync($"/api/allergies/patient/{PatientId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var records = await response.Content.ReadFromJsonAsync<List<AllergyRecord>>();
        Assert.NotNull(records);
        Assert.Contains(records, r => r.Substance == "Penicillin");
    }

    [Fact]
    public async Task GetAllergies_UnknownPatient_ReturnsEmptyList()
    {
        var response = await _client.GetAsync($"/api/allergies/patient/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var records = await response.Content.ReadFromJsonAsync<List<AllergyRecord>>();
        Assert.Empty(records!);
    }

    [Fact]
    public async Task GetAllergies_WithValidConsent_ReturnsAllergies()
    {
        // Patient grants consent to hospital-1 and records an allergy (nothing is seeded).
        await _client.PostAsJsonAsync("/api/consents",
            new DataConsent { PatientId = PatientId, OrganizationId = "hospital-1" });
        await _client.PostAsJsonAsync("/api/allergies", new AllergyRecord
        {
            PatientId = PatientId, Substance = "Penicillin", SnomedCode = "372687004", Source = "Patient PHR"
        });

        var response = await _client.GetAsync(
            $"/api/allergies/patient/{PatientId}?orgId=hospital-1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var records = await response.Content.ReadFromJsonAsync<List<AllergyRecord>>();
        Assert.NotEmpty(records!);
    }

    [Fact]
    public async Task GetAllergies_WithoutConsent_ReturnsForbidden()
    {
        var response = await _client.GetAsync(
            $"/api/allergies/patient/{PatientId}?orgId=unknown-org");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PostAllergy_CreatesRecord()
    {
        var newAllergy = new AllergyRecord
        {
            PatientId = PatientId,
            Substance = "Aspirin",
            SnomedCode = "293586001",
            Source = "Patient PHR"
        };

        var response = await _client.PostAsJsonAsync("/api/allergies", newAllergy);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<AllergyRecord>();
        Assert.Equal("Aspirin", created!.Substance);
        Assert.NotEqual(Guid.Empty, created.Id);
    }

    [Fact]
    public async Task DeleteAllergy_KnownId_ReturnsNoContent()
    {
        // Nothing is seeded — create an allergy, then delete it.
        var created = await (await _client.PostAsJsonAsync("/api/allergies", new AllergyRecord
        {
            PatientId = PatientId, Substance = "Aspirin", SnomedCode = "293586001", Source = "Patient PHR"
        })).Content.ReadFromJsonAsync<AllergyRecord>();

        var response = await _client.DeleteAsync($"/api/allergies/{created!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAllergy_UnknownId_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync($"/api/allergies/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public void Dispose() => _factory.Dispose();
}
