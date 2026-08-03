
namespace MatchmakingEngine.Application.Interfaces;

public interface ILeaderboardService
{
    Task UpdatePlayerMmrAsync(Guid playerId, int mmr);
    Task<IEnumerable<(Guid playerId, int mmr)>> GetTopPlayersAsync(int count = 100);
    Task<long?> GetPlayerRankAsync(Guid playerId);
}
