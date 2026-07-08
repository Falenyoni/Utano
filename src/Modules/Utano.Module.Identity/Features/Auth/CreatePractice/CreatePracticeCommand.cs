using MediatR;

namespace Utano.Module.Identity.Features.Auth.CreatePractice;

public record CreatePracticeCommand(
    string Name,
    string ContactEmail,
    string ContactPhone,
    string PhysicalAddress,
    string AdminFirstName,
    string AdminLastName,
    string AdminEmail,
    string AdminPassword
) : IRequest<CreatePracticeResponse>;
