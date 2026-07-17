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

        // Minimal seed: only the patient. Allergies, consents and lab results are
        // added live during the demo (each anchored in DKG at that point), so the
        // full data → DKG → ZKP flow is shown rather than pre-seeded.

        db.SaveChanges();
        return true;
    }
}
