namespace EHealth.PatientApi.Models;

public enum Gender { Male, Female, Other }

public class Patient
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public DateOnly DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public string Email { get; set; } = default!;
}
