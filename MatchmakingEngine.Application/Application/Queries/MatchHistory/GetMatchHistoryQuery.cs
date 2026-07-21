
namespace MatchmakingEngine.Application.Application.Queries.MatchHistory;

public record PlayerDto(Guid Id, string Username, int Mmr);

public record MatchDto(Guid Id, string Status, PlayerDto Player1, PlayerDto Player2);
