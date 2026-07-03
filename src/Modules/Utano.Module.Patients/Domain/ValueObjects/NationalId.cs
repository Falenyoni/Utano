namespace Utano.Module.Patients.Domain.ValueObjects;

public record NationalId
{
    private NationalId() { }

    public string Value { get; init; }
    public static NationalId Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("National ID cannot be empty.", nameof(value));
        return new NationalId
        {
            Value = value.Trim().ToUpper()
        };
    }
}