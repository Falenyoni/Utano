namespace Utano.Module.Patients.Domain.ValueObjects;

public record FullName
{
    private FullName() { }

    public string FirstName { get; init; }
    public string LastName { get; init; }
    public string MiddleName { get; init; }

    public static FullName Create(string firstName, string lastName, string middleName = "")
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be empty.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name cannot be empty.", nameof(lastName));
        return new FullName
        {
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            MiddleName = middleName.Trim()
        };
    }

    public string Display => string.IsNullOrWhiteSpace(MiddleName)
        ? $"{FirstName} {LastName}"
        : $"{FirstName} {MiddleName} {LastName}";
}