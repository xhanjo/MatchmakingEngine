using FluentValidation;

namespace MatchmakingEngine.Application.Commands.Players;

public class RegisterPlayerCommandValidator : AbstractValidator<RegisterPlayerCommand>
{
    public RegisterPlayerCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters.")
            .MaximumLength(50).WithMessage("Username cannot exceed 50 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters.");

        RuleFor(x => x.Region)
            .IsInEnum().WithMessage("Invalid region specified");
    }
}
