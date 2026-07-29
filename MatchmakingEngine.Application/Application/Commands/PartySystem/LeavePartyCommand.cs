using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Domain.Exceptions;
using System.Drawing;
using MediatR;

namespace MatchmakingEngine.Application.Application.Commands.PartySystem;

public record LeavePartyCommand(Guid PlayerId) : IRequest<bool>;

public class LeavePartyCommandHandler : IRequestHandler<LeavePartyCommand, bool>
{
    private readonly IPartyRepository _partyRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LeavePartyCommandHandler(IPartyRepository partyRepository, IUnitOfWork unitOfWork)
    {
        _partyRepository = partyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(LeavePartyCommand request, CancellationToken cancellationToken)
    {
        var party = await _partyRepository.GetPartyByPlayerIdAsync(request.PlayerId, trackChanges: true, cancellationToken);
        if (party == null)
            throw new NotFoundException("You are not in a party.");

        bool partyDisbanded = false;

        if (party.LeaderId == request.PlayerId)
        {
            _partyRepository.Delete(party);
            partyDisbanded = true;
        }
        else
        {
            var member = party.Members.First(m => m.PlayerId == request.PlayerId);
            party.Members.Remove(member);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return partyDisbanded;
    }
}
