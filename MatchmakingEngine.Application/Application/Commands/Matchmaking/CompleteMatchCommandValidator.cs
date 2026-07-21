using FluentValidation;

namespace MatchmakingEngine.Application.Commands.Matchmaking;

public class CompleteMatchCommandValidator : AbstractValidator<CompleteMatchCommand>
{
    public CompleteMatchCommandValidator()
    {
        RuleFor(x => x.MatchId)
            .NotEmpty().WithMessage("Match ID is required and cannot be empty.");

        RuleFor(x => x.WinnerId)
            .NotEmpty().WithMessage("Winner ID is required and cannot be empty.");
    }
}
