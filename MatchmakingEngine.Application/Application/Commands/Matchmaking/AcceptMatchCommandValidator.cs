using FluentValidation;
using MatchmakingEngine.Application.Application.Commands.Matchmaking;

namespace MatchmakingEngine.Application.Commands.Matchmaking;

public class AcceptMatchCommandValidator : AbstractValidator<AcceptMatchCommand>
{
    public AcceptMatchCommandValidator()
    {
        RuleFor(x => x.PlayerId)
            .NotEmpty().WithMessage("PlayerId cannot be empty.");

        RuleFor(x => x.MatchId)
            .NotEmpty().WithMessage("MatchId cannot be empty.");
    }
}