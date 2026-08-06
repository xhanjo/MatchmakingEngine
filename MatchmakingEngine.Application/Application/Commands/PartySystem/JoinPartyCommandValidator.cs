using FluentValidation;

namespace MatchmakingEngine.Application.Application.Commands.PartySystem;

public class JoinPartyCommandValidator : AbstractValidator<JoinPartyCommand>
{
    public JoinPartyCommandValidator()
    {
        RuleFor(x => x.PlayerId).NotEmpty().WithMessage("PlayerId cannot be empty.");
        RuleFor(x => x.PartyId).NotEmpty().WithMessage("PartyId cannot be empty.");
    }
}