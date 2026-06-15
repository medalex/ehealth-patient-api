using EHealth.PatientApi.Models;

namespace EHealth.PatientApi.Data;

public static class Seeder
{
    private static readonly Guid Pat1 = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public static void Seed(AppDbContext db)
    {
        if (db.Patients.Any()) return;

        db.Patients.Add(new Patient
        {
            Id = Pat1,
            FirstName = "Emily",
            LastName = "Carter",
            DateOfBirth = new DateOnly(1985, 3, 15),
            Gender = Gender.Female,
            Email = "emily.carter@example.com"
        });

        db.AllergyRecords.Add(new AllergyRecord
        {
            Id = Guid.Parse("00000000-0000-0000-0003-000000000002"),
            PatientId = Pat1,
            Substance = "Penicillin",
            SnomedCode = "372687004",
            Source = "Patient PHR",
            RecordedAt = DateTime.UtcNow.AddMonths(-6)
        });

        db.DataConsents.AddRange(
            new DataConsent
            {
                Id = Guid.Parse("00000000-0000-0000-0004-000000000001"),
                PatientId = Pat1,
                OrganizationId = "hospital-1",
                GrantedAt = DateTime.UtcNow.AddMonths(-1)
            },
            new DataConsent
            {
                Id = Guid.Parse("00000000-0000-0000-0004-000000000002"),
                PatientId = Pat1,
                OrganizationId = "lab-1",
                GrantedAt = DateTime.UtcNow.AddMonths(-1)
            },
            new DataConsent
            {
                Id = Guid.Parse("00000000-0000-0000-0004-000000000003"),
                PatientId = Pat1,
                OrganizationId = "pharmacy-1",
                GrantedAt = DateTime.UtcNow.AddMonths(-1)
            }
        );

        db.SaveChanges();
    }
}
