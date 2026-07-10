using MediatR;
using Utano.Module.Core.Exceptions;
using Utano.Module.Patients.Domain.Interfaces;

namespace Utano.Module.Patients.Features.MedicalAids.ToggleMedicalAid;

public class ActivateMedicalAidHandler(IMedicalAidRepository repository)
    : IRequestHandler<ActivateMedicalAidCommand>
{
    public async Task Handle(ActivateMedicalAidCommand command, CancellationToken cancellationToken)
    {
        var aid = await repository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new UtanoDomainException("Medical aid scheme not found.");
        aid.Activate();
        await repository.SaveAsync(cancellationToken);
    }
}

public class DeactivateMedicalAidHandler(IMedicalAidRepository repository)
    : IRequestHandler<DeactivateMedicalAidCommand>
{
    public async Task Handle(DeactivateMedicalAidCommand command, CancellationToken cancellationToken)
    {
        var aid = await repository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new UtanoDomainException("Medical aid scheme not found.");
        aid.Deactivate();
        await repository.SaveAsync(cancellationToken);
    }
}
