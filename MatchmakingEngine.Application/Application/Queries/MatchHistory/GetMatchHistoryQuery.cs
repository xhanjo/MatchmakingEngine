
using MatchmakingEngine.Domain;

namespace MatchmakingEngine.Application.Application.Queries.MatchHistory;

public record PlayerDto(Guid Id, string Username, int Mmr);

public record MatchDto(Guid Id, MatchStatus MatchStatus, DateTimeOffset CreatedAt);
