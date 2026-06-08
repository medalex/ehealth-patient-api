using EHealth.PatientApi.Data;
using EHealth.PatientApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EHealth.PatientApi.Endpoints;

public static class PatientEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/patients").WithTags("Patients");

        group.MapGet("/", async (AppDbContext db) =>
            await db.Patients.ToListAsync());

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
            await db.Patients.FindAsync(id) is { } patient
                ? Results.Ok(patient)
                : Results.NotFound());

        group.MapPost("/", async (Patient patient, AppDbContext db) =>
        {
            patient.Id = Guid.NewGuid();
            db.Patients.Add(patient);
            await db.SaveChangesAsync();
            return Results.Created($"/api/patients/{patient.Id}", patient);
        });

        group.MapPut("/{id:guid}", async (Guid id, Patient incoming, AppDbContext db) =>
        {
            var patient = await db.Patients.FindAsync(id);
            if (patient is null) return Results.NotFound();
            patient.FirstName = incoming.FirstName;
            patient.LastName = incoming.LastName;
            patient.DateOfBirth = incoming.DateOfBirth;
            patient.Gender = incoming.Gender;
            patient.Email = incoming.Email;
            await db.SaveChangesAsync();
            return Results.Ok(patient);
        });

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var patient = await db.Patients.FindAsync(id);
            if (patient is null) return Results.NotFound();
            db.Patients.Remove(patient);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
