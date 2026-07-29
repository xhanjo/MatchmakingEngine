using MatchmakingEngine.Application.DTO;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Domain;
using MatchmakingEngine.Domain.Exceptions;
using MediatR;

namespace MatchmakingEngine.Application.Application.Commands.PartySystem;

public record CreatePartyCommand(Guid LeaderId, GameMode GameMode) : IRequest<PartyDto>;

public class CreatePartyCommandHandler : IRequestHandler<CreatePartyCommand, PartyDto>
{
    private readonly IPartyRepository _partyRepository;
    private readonly IPlayerRepository _playerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePartyCommandHandler(IPartyRepository partyRepository, IPlayerRepository playerRepository, IUnitOfWork unitOfWork)
    {
        _partyRepository = partyRepository;
        _playerRepository = playerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PartyDto> Handle(CreatePartyCommand request, CancellationToken cancellationToken)
    {
        var existingParty = await _partyRepository.GetPartyByPlayerIdAsync(request.LeaderId, trackChanges: false, cancellationToken);
        if (existingParty != null)
            throw new ConflictException("You are already in a party");

        var leader = await _playerRepository.GetByIdAsync(request.LeaderId, trackChanges: false, cancellationToken);
        if (leader == null)
            throw new NotFoundException("Player not found.");

        var party = new Party
        {
            LeaderId = leader.Id,
            GameMode = request.GameMode
        };

        var leaderMemder = new PartyMember
        {
            PartyId = party.Id,
            PlayerId = leader.Id
        };
        party.Members.Add(leaderMemder);

        await _partyRepository.AddAsync(party, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new PartyDto(
            party.Id,
            leader.Id,
            leader.Username,
            party.GameMode,
            new List<PartyMembersDto> { new PartyMembersDto(leader.Id, leader.Username, leader.Mmr) }
        );
    }
}