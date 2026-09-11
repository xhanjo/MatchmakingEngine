using MatchmakingEngine.Application.DTO;
using MatchmakingEngine.Application.Interfaces;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Domain;
using MatchmakingEngine.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MatchmakingEngine.Application.Application.Commands.Matchmaking;

public class CompleteMatchCommandHandler : IRequestHandler<CompleteMatchCommand, CompleteMatchResult>
{
    private readonly IMatchRepository _matchRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CompleteMatchCommandHandler> _logger;
    private readonly ICacheService _cacheService;
    private readonly ILeaderboardService _leaderboardService;

    public CompleteMatchCommandHandler(
        IMatchRepository matchRepository,
        IUnitOfWork unitOfWork,
        ILogger<CompleteMatchCommandHandler> logger,
        ICacheService cacheService,
        ILeaderboardService leaderboardService)
    {
        _matchRepository = matchRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cacheService = cacheService;
        _leaderboardService = leaderboardService;
    }

    public async Task<CompleteMatchResult> Handle(CompleteMatchCommand request, CancellationToken cancellationToken)
    {

        var match = await _matchRepository.GetByIdWithPlayersAsync(request.MatchId, trackChanges: true);
        
        if (match == null)
            throw new NotFoundException($"Match with ID {request.MatchId} was not found.");

        if (match.Status != MatchStatus.Accepted &&
            match.Status != MatchStatus.MapVeto &&
            match.Status != MatchStatus.StartingServer)
        {
            throw new ConflictException("Match has not started yet or is already finished.");
        }

        match.Status = MatchStatus.Finished;

        var random = new Random();

        int team1Kills = random.Next(5, 31);
        int team2Kills = random.Next(5, 31);
        if (team1Kills == team2Kills) team1Kills++; 

        int winningTeam = team1Kills > team2Kills ? 1 : 2;

        var team1Players = match.Players.Where(p => p.Team == 1).ToList();
        var team2Players = match.Players.Where(p => p.Team == 2).ToList();

        int team1Deaths = team2Kills;
        int team2Deaths = team1Kills;

        void DistributeStat(List<MatchPlayer> teamPlayers, int totalStat, Action<MatchPlayer, int> setStat)
        {
            if (!teamPlayers.Any()) return;
            var remaining = totalStat;
            for (int i = 0; i < teamPlayers.Count - 1; i++)
            {
                var val = remaining > 0 ? random.Next(0, remaining + 1) : 0;
                setStat(teamPlayers[i], val);
                remaining -= val;
            }
            setStat(teamPlayers.Last(), remaining);
        }

        DistributeStat(team1Players, team1Kills, (p, v) => p.Kills = v);
        DistributeStat(team2Players, team2Kills, (p, v) => p.Kills = v);
        DistributeStat(team1Players, team1Deaths, (p, v) => p.Deaths = v);
        DistributeStat(team2Players, team2Deaths, (p, v) => p.Deaths = v);

        foreach (var p in match.Players)
        {
            p.Assists = random.Next(0, Math.Max(1, p.Kills / 2 + 1));
            p.Score = (p.Kills * 2) + p.Assists;
        }

        var maxScore = match.Players.Max(p => p.Score);
        var topScorers = match.Players.Where(p => p.Score == maxScore).ToList();
        var mvpPlayer = topScorers[random.Next(topScorers.Count)];
        mvpPlayer.IsMvp = true;

        var scoreboard = new List<PlayerStatsDto>();

        var team1AvgMmr = team1Players.Any() ? team1Players.Average(p => p.Player.Mmr) : 1000;
        var team2AvgMmr = team2Players.Any() ? team2Players.Average(p => p.Player.Mmr) : 1000;

        var expectedScore1 = 1.0 / (1.0 + Math.Pow(10.0, (team2AvgMmr - team1AvgMmr) / 400.0));
        var expectedScore2 = 1.0 / (1.0 + Math.Pow(10.0, (team1AvgMmr - team2AvgMmr) / 400.0));

        var actualScore1 = winningTeam == 1 ?  1.0 : 0.0;
        var actualScore2 = winningTeam == 2 ? 1.0 : 0.0;

        int kFactor = 50;
        int team1MmrChange = (int)Math.Round(kFactor * (actualScore1 - expectedScore1));
        int team2MmrChange = (int)Math.Round(kFactor * (actualScore2 - expectedScore2));

        foreach (var p in match.Players)
        {
            int mmrChange = p.Team == 1 ? team1MmrChange : team2MmrChange;
            if (p.Player.Mmr + mmrChange < 0)
                mmrChange = -p.Player.Mmr;

            p.Player.Mmr += mmrChange;

            p.Player.TrustFactor = Math.Min(1.0, p.Player.TrustFactor + 0.02);

            scoreboard.Add(new PlayerStatsDto(
                p.PlayerId,
                p.Player.Username,
                p.Team,
                p.Kills,
                p.Deaths,
                p.Assists,
                p.Score,
                p.IsMvp,
                mmrChange
            ));
        }

        await _cacheService.RemoveAsync("all_players", cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var p in match.Players)
        {
            await _leaderboardService.UpdatePlayerMmrAsync(p.PlayerId, p.Player.Mmr);
        }

        _logger.LogInformation("Match {MatchId} completed. Winner: Team {WinningTeam}. MVP: {Mvp}",
            match.Id, winningTeam, mvpPlayer?.Player.Username);

        return new CompleteMatchResult(
            "Match completed",
            winningTeam,
            mvpPlayer?.PlayerId ?? Guid.Empty,
            mvpPlayer?.Player.Username ?? "Unknown",
            scoreboard.OrderByDescending(s => s.Score).ToList(),
            match.Status
        );
    }
}

