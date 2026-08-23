using Utano.Module.Identity.Domain.Entities;

namespace Utano.Module.Identity.Domain.Interfaces;

public interface IUserReadRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByIdWithTokensAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsInPracticeAsync(string email, Guid practiceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetByRoleAsync(Guid practiceId, string role, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetAllByPracticeAsync(Guid practiceId, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PasswordResetToken?> GetValidPasswordResetTokenAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<EmailVerificationToken?> GetValidEmailVerificationTokenAsync(string tokenHash, CancellationToken cancellationToken = default);
}
