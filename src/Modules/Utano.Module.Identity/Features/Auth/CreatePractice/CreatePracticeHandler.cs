using FluentValidation;
using MediatR;
using Utano.Module.Core.Exceptions;
using Utano.Module.Identity.Domain.Entities;
using Utano.Module.Identity.Domain.Enums;
using Utano.Module.Identity.Domain.Interfaces;

namespace Utano.Module.Identity.Features.Auth.CreatePractice;

public class CreatePracticeHandler(
    IPracticeRepository practiceRepository,
    IUserWriteRepository userWriteRepository,
    IPasswordService passwordService,
    IValidator<CreatePracticeCommand> validator)
    : IRequestHandler<CreatePracticeCommand, CreatePracticeResponse>
{
    public async Task<CreatePracticeResponse> Handle(
        CreatePracticeCommand command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            throw new UtanoDomainException(validation.Errors[0].ErrorMessage);

        var practice = Practice.Create(
            command.Name, command.ContactEmail,
            command.ContactPhone, command.PhysicalAddress);

        await practiceRepository.AddAsync(practice, cancellationToken);

        var passwordHash = passwordService.Hash(command.AdminPassword);

        var admin = User.Create(
            practice.Id,
            command.AdminFirstName,
            command.AdminLastName,
            command.AdminEmail,
            passwordHash,
            UserRole.Admin);

        await userWriteRepository.AddAsync(admin, cancellationToken);

        return new CreatePracticeResponse(practice.Id, practice.Name, admin.Id, admin.Email.Value);
    }
}
