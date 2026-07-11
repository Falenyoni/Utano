using MediatR;
using Utano.Module.Core.Services;
using Utano.Module.Identity.Domain.Enums;
using Utano.Module.Identity.Domain.Interfaces;

namespace Utano.Module.Identity.Features.Users.GetDoctors;

public class GetDoctorsHandler(
    IUserReadRepository readRepository,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetDoctorsQuery, IReadOnlyList<DoctorResponse>>
{
    public async Task<IReadOnlyList<DoctorResponse>> Handle(
        GetDoctorsQuery query, CancellationToken cancellationToken)
    {
        var doctors = await readRepository.GetByRoleAsync(
            currentUserService.PracticeId, UserRole.Doctor, cancellationToken);
        var nurses = await readRepository.GetByRoleAsync(
            currentUserService.PracticeId, UserRole.Nurse, cancellationToken);

        return doctors.Concat(nurses)
            .OrderBy(u => u.FullName)
            .Select(d => new DoctorResponse(d.Id, d.FullName, d.Email.Value))
            .ToList()
            .AsReadOnly();
    }
}
