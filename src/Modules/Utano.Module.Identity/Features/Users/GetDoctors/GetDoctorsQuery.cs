using MediatR;

namespace Utano.Module.Identity.Features.Users.GetDoctors;

public record GetDoctorsQuery : IRequest<IReadOnlyList<DoctorResponse>>;
