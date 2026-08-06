using FluentValidation;

namespace MatchmakingEngine.Application.Application.Commands.Matchmaking;

public class LeaveQueueCommandValidator : AbstractValidator<LeaveQueueCommand>
{
    public LeaveQueueCommandValidator()
    {
        RuleFor(x => x.PlayerId).NotEmpty().WithMessage("PlayerId cannot be empty.");
    }
}