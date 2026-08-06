using FluentValidation;

namespace MatchmakingEngine.Application.Application.Commands.PartySystem;

public class InviteToPartyCommandValidator : AbstractValidator<InviteToPartyCommand>
{
    public InviteToPartyCommandValidator()
    {
        RuleFor(x => x.InviteId).NotEmpty().WithMessage("InviteId cannot be empty.");
        RuleFor(x => x.FriendId).NotEmpty().WithMessage("FriendId cannot be empty.");
        RuleFor(x => x.FriendId).NotEqual(x => x.InviteId).WithMessage("You cannot invite yourself to a party.");
    }
}