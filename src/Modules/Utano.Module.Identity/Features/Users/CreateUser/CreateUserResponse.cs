namespace Utano.Module.Identity.Features.Users.CreateUser;

public record CreateUserResponse(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    string Status,
    DateTimeOffset CreatedAt
);
