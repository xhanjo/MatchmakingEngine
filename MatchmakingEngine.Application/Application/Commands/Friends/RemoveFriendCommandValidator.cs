using FluentValidation;

namespace MatchmakingEngine.Application.Application.Commands.Friends;

public class RemoveFriendCommandValidator : AbstractValidator<RemoveFriendCommand>
{
    public RemoveFriendCommandValidator()
    {
        RuleFor(x => x.FriendshipId).NotEmpty().WithMessage("FriendshipId cannot be empty.");
        RuleFor(x => x.PlayerId).NotEmpty().WithMessage("PlayerId cannot be empty.");
    }
}