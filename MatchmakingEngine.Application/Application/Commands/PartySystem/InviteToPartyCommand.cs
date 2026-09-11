using MatchmakingEngine.Application.Interfaces.Repositories;
using MediatR;
using MatchmakingEngine.Domain.Exceptions;

namespace MatchmakingEngine.Application.Application.Commands.PartySystem;

public record InviteToPartyResult(Guid PartyId);
public record InviteToPartyCommand(Guid InviteId, Guid FriendId) : IRequest<InviteToPartyResult>;
public record PartyInviteCache(Guid PartyId, Guid SenderId);

public class InviteToPartyCommandHandler : IRequestHandler<InviteToPartyCommand, InviteToPartyResult>
{
    private readonly IPartyRepository _partyRepository;
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly Interfaces.ICacheService _cacheService;

    public InviteToPartyCommandHandler(IPartyRepository partyRepository, IFriendshipRepository friendshipRepository, Interfaces.ICacheService cacheService)
    {
        _partyRepository = partyRepository;
        _friendshipRepository = friendshipRepository;
        _cacheService = cacheService;
    }

    public async Task<InviteToPartyResult> Handle(InviteToPartyCommand request, CancellationToken cancellationToken)
    {
        var party = await _partyRepository.GetPartyByPlayerIdAsync(request.InviteId, trackChanges: false, cancellationToken);
        if (party == null || party.LeaderId != request.InviteId)
            throw new ConflictException("You must be a party leader to invite friends.");

        if (party.GameMode == Domain.GameMode.Solo)
            throw new ConflictException("Cannot invite friends to a Solo party.");

        if (party.Members is { Count: >= 2 })
            throw new ConflictException("The party is full.");

        var friendship = await _friendshipRepository.GetBetweenPlayersAsync(request.InviteId, request.FriendId, cancellationToken);
        if (friendship == null || friendship.Status != Domain.FriendshipStatus.Accepted)
            throw new ConflictException("You can only invite accepted friends.");

        var cacheKey = $"party_invite:{request.FriendId}:{party.Id}";
        await _cacheService.GetOrCreateAsync(cacheKey, () => Task.FromResult(new PartyInviteCache(party.Id, request.InviteId))!, TimeSpan.FromMinutes(5));

        return new InviteToPartyResult(party.Id);
    }
}