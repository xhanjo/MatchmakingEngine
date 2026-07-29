using MatchmakingEngine.Application.Application.Queries.MatchHistory;
using MatchmakingEngine.Application.DTO;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Domain;
using MatchmakingEngine.Domain.Exceptions;
using MediatR;
using System.Runtime.CompilerServices;

namespace MatchmakingEngine.Application.Application.Commands.PartySystem;

public record JoinPartyCommand(Guid PlayerId, Guid PartyId) : IRequest<PartyDto>;

public class JoinPartyCommandHandler : IRequestHandler<JoinPartyCommand, PartyDto>
{
    private readonly IPartyRepository _partyRepository;
    private readonly IPlayerRepository _playerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public JoinPartyCommandHandler(IPartyRepository partyRepository, IPlayerRepository playerRepository, IUnitOfWork unitOfWork)
    {
        _partyRepository = partyRepository;
        _playerRepository = playerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PartyDto> Handle(JoinPartyCommand request, CancellationToken cancellationToken)
    {
        var existingParty = await _partyRepository.GetPartyByPlayerIdAsync(request.PlayerId, trackChanges: false, cancellationToken);
        if (existingParty != null)
            throw new ConflictException("You are already in a party.");

        var party = await _partyRepository.GetByIdWithMembersAsync(request.PartyId, trackChanges: true, cancellationToken);
        if (party == null)
            throw new NotFoundException("Party not found.");

        if (party.Members.Count >= 2)
            throw new ConflictException("The party is full.");

        var player = await _playerRepository.GetByIdAsync(request.PlayerId, trackChanges: false, cancellationToken);
        if (player == null)
            throw new NotFoundException("Player not found.");

        var newMember = new PartyMember
        {
            PartyId = party.Id,
            PlayerId = player.Id,
            Player = player
        };
        party.Members.Add(newMember);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var memberDtos = party.Members
            .Select(m =>
            new PartyMembersDto(
                m.PlayerId,
                m.Player.Username,
                m.Player.Mmr)
            ).ToList();

        return new PartyDto(party.Id, party.LeaderId, party.Leader.Username, party.GameMode, memberDtos);
    }
}
