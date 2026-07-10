using FluentValidation;
using MediatR;
using Utano.Module.Core.Exceptions;
using Utano.Module.Core.Services;
using Utano.Module.Identity.Domain.Entities;
using Utano.Module.Identity.Domain.Enums;
using Utano.Module.Identity.Domain.Interfaces;

namespace Utano.Module.Identity.Features.Users.CreateUser;

public class CreateUserHandler(
    IUserWriteRepository writeRepository,
    IUserReadRepository readRepository,
    IPasswordService passwordService,
    ICurrentUserService currentUserService,
    IValidator<CreateUserCommand> validator)
    : IRequestHandler<CreateUserCommand, CreateUserResponse>
{
    public async Task<CreateUserResponse> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            throw new UtanoDomainException(validation.Errors[0].ErrorMessage);

        var emailExists = await readRepository.EmailExistsAsync(command.Email, cancellationToken);
        if (emailExists)
            throw new UtanoDomainException("A user with this email already exists.");

        var role = Enum.Parse<UserRole>(command.Role, ignoreCase: true);
        var passwordHash = passwordService.Hash(command.Password);

        var user = User.Create(
            currentUserService.PracticeId,
            command.FirstName,
            command.LastName,
            command.Email,
            passwordHash,
            role);

        await writeRepository.AddAsync(user, cancellationToken);

        return new CreateUserResponse(
            user.Id,
            user.FullName,
            user.Email.Value,
            user.Role.ToString(),
            user.Status.ToString(),
            user.CreatedAt);
    }
}
