using FluentValidation;

namespace MatchmakingEngine.Application.Application.Commands.Matchmaking;

public class CompleteMatchCommandValidator : AbstractValidator<CompleteMatchCommand>
{
    public CompleteMatchCommandValidator()
    {
        RuleFor(x => x.MatchId)
            .NotEmpty().WithMessage("Match ID is required and cannot be empty.");
    }
}
