using FluentValidation;

namespace MatchmakingEngine.Application.Application.Commands.Friends;

public class DeclineFriendRequestCommandValidator : AbstractValidator<DeclineFriendRequestCommand>
{
    public DeclineFriendRequestCommandValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty().WithMessage("RequestId cannot be empty.");
        RuleFor(x => x.PlayerId).NotEmpty().WithMessage("PlayerId cannot be empty.");
    }
}
