namespace Utano.Module.Identity.Features.Auth.CreatePractice;

public record CreatePracticeResponse(
    Guid PracticeId,
    string PracticeName,
    Guid AdminUserId,
    string AdminEmail
);
