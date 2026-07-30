using MatchmakingEngine.Application.Interfaces;
using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Domain;
using MatchmakingEngine.Domain.Exceptions;
using MatchmakingEngine.DTO;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MatchmakingEngine.Application.Commands.Matchmaking;

public class CompleteMatchCommandHandler : IRequestHandler<CompleteMatchCommand, CompleteMatchResult>
{
    private readonly IMatchRepository _matchRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CompleteMatchCommandHandler> _logger;

    public CompleteMatchCommandHandler(
        IMatchRepository matchRepository,
        IUnitOfWork unitOfWork,
        ILogger<CompleteMatchCommandHandler> logger)
    {
        _matchRepository = matchRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CompleteMatchResult> Handle(CompleteMatchCommand request, CancellationToken cancellationToken)
    {

        var match = await _matchRepository.GetByIdWithPlayersAsync(request.MatchId, trackChanges: true);
        
        if (match == null)
            throw new NotFoundException($"Match with ID {request.MatchId} was not found.");

        if (match.Status != MatchStatus.Accepted)
            throw new ConflictException("Match has not started yet or is already finished.");

        match.Status = MatchStatus.Finished;

        var random = new Random();

        int winningTeam = random.Next(1, 3);

        var scoreboard = new List<PlayerStatsDto>();
        MatchPlayer? mvpPlayer = null;

        foreach (var p in match.Players)
        {
            p.Kills = random.Next(0, 31);
            p.Deaths = random.Next(0, 31);
            p.Assists = random.Next(0, 16);
            p.Score = (p.Kills*2) + p.Assists;

            if (mvpPlayer == null || p.Score > mvpPlayer.Score)
                mvpPlayer = p;

            int mmrChange = p.Team == winningTeam ? 25 : -25;
            p.Player.Mmr += mmrChange;

            scoreboard.Add(new PlayerStatsDto(
                p.Id,
                p.Player.Username,
                p.Team,
                p.Kills,
                p.Deaths,
                p.Assists,
                p.Score,
                false,
                mmrChange
                ));
        }

        if (mvpPlayer != null)
        {
            mvpPlayer.IsMvp = true;

            var mvpIndex = scoreboard.FindIndex(s => s.PlayerId == mvpPlayer.Id);
            if (mvpIndex != -1)
            {
                scoreboard[mvpIndex] = scoreboard[mvpIndex] with { IsMvp = true };
            } 
                
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

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

