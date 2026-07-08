using Utano.Module.Core.Exceptions;

namespace Utano.Module.Identity.Domain.ValueObjects;

public record Email
{
    private Email() { }

    public string Value { get; init; } = null!;

    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new UtanoDomainException("Email is required.");

        var normalised = value.Trim().ToLower();

        if (!normalised.Contains('@') || !normalised.Contains('.'))
            throw new UtanoDomainException("Email is not valid.");

        return new Email { Value = normalised };
    }
}
