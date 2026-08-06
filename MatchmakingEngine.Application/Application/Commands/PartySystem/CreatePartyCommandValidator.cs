using FluentValidation;

namespace MatchmakingEngine.Application.Application.Commands.PartySystem;

public class CreatePartyCommandValidator : AbstractValidator<CreatePartyCommand>
{
    public CreatePartyCommandValidator()
    {
        RuleFor(x => x.LeaderId).NotEmpty().WithMessage("LeaderId cannot be empty.");
        RuleFor(x => x.GameMode).IsInEnum().WithMessage("Invalid GameMode specified.");
    }
}