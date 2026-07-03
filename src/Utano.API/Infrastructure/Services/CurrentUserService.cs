using Utano.Module.Core.Services;

namespace Utano.API.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    // TODO: replace with real JWT claim extraction once auth is wired up
    public Guid UserId { get; } = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public string Email { get; } = "admin@utano.health";
    public string FullName { get; } = "System Admin";
    public string Role { get; } = "Admin";
    public Guid PracticeId { get; } = Guid.Parse("00000000-0000-0000-0000-000000000002");
}
