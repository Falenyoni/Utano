using Utano.Module.Patients.Domain.Enums;

namespace Utano.Module.Patients.Domain.ValueObjects;

public record PatientIdentifier
{
    private PatientIdentifier() { }

    public PatientIdentifierType Type { get; init; }
    public string? Value { get; init; }

    public static PatientIdentifier Create(PatientIdentifierType type, string? value)
    {
        if (type == PatientIdentifierType.Pending)
        {
            if (!string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("A Pending identifier cannot carry a value.", nameof(value));
            return new PatientIdentifier { Type = type, Value = null };
        }

        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{type} requires a value.", nameof(value));

        return new PatientIdentifier { Type = type, Value = value.Trim().ToUpper() };
    }

    public static PatientIdentifier Pending() => new() { Type = PatientIdentifierType.Pending, Value = null };
}
