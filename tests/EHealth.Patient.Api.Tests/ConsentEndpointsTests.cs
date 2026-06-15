using System.Net;
using System.Net.Http.Json;
using EHealth.PatientApi.Models;
using EHealth.PatientApi.Tests.Helpers;

namespace EHealth.PatientApi.Tests;

public class ConsentEndpointsTests : IDisposable
{
    private readonly TestFactory _factory = new();
    private readonly HttpClient _client;
    private static readonly Guid PatientId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public ConsentEndpointsTests()
    {
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetConsents_ReturnsSeededConsent()
    {
        var response = await _client.GetAsync($"/api/consents/patient/{PatientId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var consents = await response.Content.ReadFromJsonAsync<List<DataConsent>>();
        Assert.NotNull(consents);
        Assert.Equal(3, consents.Count);
        Assert.Contains(consents, c => c.OrganizationId == "hospital-1");
        Assert.Contains(consents, c => c.OrganizationId == "lab-1");
        Assert.Contains(consents, c => c.OrganizationId == "pharmacy-1");
    }

    [Fact]
    public async Task GetConsents_UnknownPatient_ReturnsEmptyList()
    {
        var response = await _client.GetAsync($"/api/consents/patient/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var consents = await response.Content.ReadFromJsonAsync<List<DataConsent>>();
        Assert.Empty(consents!);
    }

    [Fact]
    public async Task PostConsent_CreatesConsent()
    {
        var newConsent = new DataConsent
        {
            PatientId = PatientId,
            OrganizationId = "lab-1"
        };

        var response = await _client.PostAsJsonAsync("/api/consents", newConsent);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<DataConsent>();
        Assert.Equal("lab-1", created!.OrganizationId);
        Assert.NotEqual(Guid.Empty, created.Id);
    }

    [Fact]
    public async Task DeleteConsent_KnownId_ReturnsNoContent()
    {
        var consents = await _client.GetFromJsonAsync<List<DataConsent>>(
            $"/api/consents/patient/{PatientId}");
        var id = consents![0].Id;

        var response = await _client.DeleteAsync($"/api/consents/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteConsent_UnknownId_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync($"/api/consents/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public void Dispose() => _factory.Dispose();
}
