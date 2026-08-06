using FluentValidation;

namespace MatchmakingEngine.Application.Application.Commands.PartySystem;

public class LeavePartyCommandValidator : AbstractValidator<LeavePartyCommand>
{
    public LeavePartyCommandValidator()
    {
        RuleFor(x => x.PlayerId).NotEmpty().WithMessage("PlayerId cannot be empty.");
    }
}