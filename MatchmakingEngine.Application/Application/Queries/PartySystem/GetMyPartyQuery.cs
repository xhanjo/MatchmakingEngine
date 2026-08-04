using MatchmakingEngine.Application.DTO;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Domain.Exceptions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace MatchmakingEngine.Application.Application.Queries.PartySystem;

public record GetMyPartyQuery(Guid PlayerId) : IRequest<PartyDto>;

public class GetMyPartyQueryHandler : IRequestHandler<GetMyPartyQuery, PartyDto>
{
    private readonly IPartyRepository _partyRepository;
    public GetMyPartyQueryHandler(IPartyRepository partyRepository)
    {
        _partyRepository = partyRepository;
    }

    public async Task<PartyDto> Handle(GetMyPartyQuery request, CancellationToken cancellationToken)
    {
        var party = await _partyRepository.GetPartyByPlayerIdAsync(request.PlayerId, trackChanges: false, cancellationToken);

        if (party == null)
            return null!;

        var memberDtos = party.Members
            .Select(m => new PartyMembersDto(m.PlayerId, m.Player.Username, m.Player.Mmr))
            .ToList();

        return new PartyDto(party.Id, party.LeaderId, party.Leader.Username, party.GameMode, memberDtos);
    }
}