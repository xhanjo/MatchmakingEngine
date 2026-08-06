using FluentValidation;

namespace MatchmakingEngine.Application.Application.Commands.Matchmaking;

public class BanMapCommandValidator : AbstractValidator<BanMapCommand>
{
    public BanMapCommandValidator()
    {
        RuleFor(x => x.MatchId).NotEmpty().WithMessage("MatchId cannot be empty.");
        RuleFor(x => x.PlayerId).NotEmpty().WithMessage("PlayerId cannot be empty.");
        RuleFor(x => x.MapName).NotEmpty().WithMessage("MapName cannot be empty.");
    }
}