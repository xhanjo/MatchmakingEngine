using MatchmakingEngine.Domain;

namespace MatchmakingEngine.Application.DTO;

public record PartyMembersDto(Guid PlayerId, string Username, int Mmr);
public record PartyDto(
    Guid Id,
    Guid LeaderId,
    string LeaderUsername,
    GameMode GameMode,
    List<PartyMembersDto> Members
);
