namespace Utano.Module.Core.Services;

public interface ICurrentUserService
{
    Guid UserId { get; }
    string Email { get; }
    string FullName { get; }
    string Role { get; }
    Guid PracticeId { get; }
    IReadOnlyList<string> Permissions { get; }
    bool HasPermission(string permission);
}
