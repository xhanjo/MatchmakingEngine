using FluentValidation;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Data;

namespace MatchmakingEngine.Application.Commands.Auth;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}
