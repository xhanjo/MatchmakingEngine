using FluentValidation;

namespace MatchmakingEngine.Application.Application.Commands.Friends;

public class AcceptFriendRequestCommandValidator : AbstractValidator<AcceptFriendRequestCommand>
{
    public AcceptFriendRequestCommandValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty().WithMessage("RequestId cannot be empty.");
        RuleFor(x => x.PlayerId).NotEmpty().WithMessage("PlayerId cannot be empty.");
    }
}
