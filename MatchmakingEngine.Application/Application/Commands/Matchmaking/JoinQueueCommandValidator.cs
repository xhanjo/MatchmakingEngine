using FluentValidation;
using MatchmakingEngine.Application.Application.Commands.Matchmaking;

namespace MatchmakingEngine.Application.Commands.Matchmaking;

public class JoinQueueCommandValidator : AbstractValidator<JoinQueueCommand>
{
    public JoinQueueCommandValidator()
    {
        RuleFor(x => x.PlayerId)
            .NotEmpty().WithMessage("PlayerId cannot be empty.");
    }
}