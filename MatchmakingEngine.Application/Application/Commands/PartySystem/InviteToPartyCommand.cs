using MatchmakingEngine.Application.Interfaces.Repositories;
using MediatR;
using MatchmakingEngine.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace MatchmakingEngine.Application.Application.Commands.PartySystem;

public record InviteToPartyResult(Guid PartyId);
public record InviteToPartyCommand(Guid InviteId, Guid FriendId) : IRequest<InviteToPartyResult>;

public class InviteToPartyCommandHandler : IRequestHandler<InviteToPartyCommand, InviteToPartyResult>
{
    private readonly IPartyRepository _partyRepository;
    private readonly IFriendshipRepository _friendshipRepository;

    public InviteToPartyCommandHandler(IPartyRepository partyRepository, IFriendshipRepository friendshipRepository)
    {
        _partyRepository = partyRepository;
        _friendshipRepository = friendshipRepository;
    }

    public async Task<InviteToPartyResult> Handle(InviteToPartyCommand request, CancellationToken cancellationToken)
    {
        var party = await _partyRepository.GetPartyByPlayerIdAsync(request.InviteId, trackChanges: false, cancellationToken);
        if (party == null || party.LeaderId != request.InviteId)
            throw new ConflictException("You must be a party leader to invite friends.");

        if (party.GameMode == MatchmakingEngine.Domain.GameMode.Solo)
            throw new ConflictException("Cannot invite friends to a Solo party.");

        if (party.Members.Count >= 2)
            throw new ConflictException("The party is full.");

        var friendship = await _friendshipRepository.GetBetweenPlayersAsync(request.InviteId, request.FriendId, cancellationToken);
        if (friendship == null || friendship.Status != MatchmakingEngine.Domain.FriendshipStatus.Accepted)
            throw new ConflictException("You can only invite accepted friends.");

        return new InviteToPartyResult(party.Id);
    }
}