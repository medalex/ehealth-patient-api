using EHealth.PatientApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EHealth.PatientApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<AllergyRecord> AllergyRecords => Set<AllergyRecord>();
    public DbSet<DataConsent> DataConsents => Set<DataConsent>();
}
