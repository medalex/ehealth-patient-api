using EHealth.PatientApi.Models;

namespace EHealth.PatientApi.Data;

public static class Seeder
{
    private static readonly Guid Pat1 = Guid.Parse("00000000-0000-0000-0000-000000000001");

    // Returns true if the database was seeded (false if it was already populated).
    public static bool Seed(AppDbContext db)
    {
        if (db.Patients.Any()) return false;

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

        // No data-sharing consents are seeded: the demo starts with the patient
        // having granted NO consent. The patient grants consent via the portal
        // (which anchors it in DKG), so the flow can show access denied first,
        // then granted → prescription succeeds.

        db.SaveChanges();
        return true;
    }
}
