using FluentValidation;

namespace MatchmakingEngine.Application.Application.Commands.Matchmaking;

public class DeclineMatchCommandValidator : AbstractValidator<DeclineMatchCommand>
{
    public DeclineMatchCommandValidator()
    {
        RuleFor(x => x.MatchId).NotEmpty().WithMessage("MatchId cannot be empty.");
        RuleFor(x => x.PlayerId).NotEmpty().WithMessage("PlayerId cannot be empty.");
    }
}