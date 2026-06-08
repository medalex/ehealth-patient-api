using System.Net;
using System.Net.Http.Json;
using EHealth.PatientApi.Models;
using EHealth.PatientApi.Tests.Helpers;

namespace EHealth.PatientApi.Tests;

public class PatientEndpointsTests : IDisposable
{
    private readonly TestFactory _factory = new();
    private readonly HttpClient _client;

    public PatientEndpointsTests()
    {
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetPatients_ReturnsSeededPatient()
    {
        var response = await _client.GetAsync("/api/patients");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var patients = await response.Content.ReadFromJsonAsync<List<Patient>>();
        Assert.NotNull(patients);
        Assert.Single(patients);
        Assert.Equal("Shevchenko", patients[0].LastName);
    }

    [Fact]
    public async Task GetPatient_UnknownId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/patients/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPatient_KnownId_ReturnsPatient()
    {
        var all = await _client.GetFromJsonAsync<List<Patient>>("/api/patients");
        var id = all![0].Id;

        var response = await _client.GetAsync($"/api/patients/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var patient = await response.Content.ReadFromJsonAsync<Patient>();
        Assert.Equal("Maria", patient!.FirstName);
    }

    [Fact]
    public async Task PostPatient_CreatesPatient()
    {
        var newPatient = new Patient
        {
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateOnly(1990, 1, 1),
            Gender = Gender.Male,
            Email = "john.doe@example.com"
        };

        var response = await _client.PostAsJsonAsync("/api/patients", newPatient);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<Patient>();
        Assert.Equal("Doe", created!.LastName);
        Assert.NotEqual(Guid.Empty, created.Id);
    }

    [Fact]
    public async Task DeletePatient_KnownId_ReturnsNoContent()
    {
        var all = await _client.GetFromJsonAsync<List<Patient>>("/api/patients");
        var id = all![0].Id;

        var response = await _client.DeleteAsync($"/api/patients/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeletePatient_UnknownId_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync($"/api/patients/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public void Dispose() => _factory.Dispose();
}
