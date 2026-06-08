namespace EHealth.PatientApi.Models;

public class DataConsent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PatientId { get; set; }
    public string OrganizationId { get; set; } = default!; // e.g. "hospital-1"
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
}
