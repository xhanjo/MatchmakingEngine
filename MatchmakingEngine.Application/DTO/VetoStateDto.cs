namespace MatchmakingEngine.Application.DTO;

public class VetoPlayerDto
{
    public Guid PlayerId { get; set; }
    public string Username { get; set; } = string.Empty;
}

public class VetoMapDto
{
    public string Name { get; set; } = string.Empty;
    public bool IsBanned { get; set; }
    public string? BannedByUsername { get; set; }
}

public class VetoStateDto
{
    public Guid MatchId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int CurrentTurnTeam { get; set; }
    public Guid? CurrentVetoTurnPlayerId { get; set; }
    public List<VetoPlayerDto> Team1 { get; set; } = new();
    public List<VetoPlayerDto> Team2 { get; set; } = new();
    public List<VetoMapDto> Maps { get; set; } = new();

    public static VetoStateDto FromMatch(Domain.Match match)
    {
        var dto = new VetoStateDto
        {
            MatchId = match.Id,
            Status = match.Status == Domain.MatchStatus.MapVeto ? "InProgress" : "Completed",
            CurrentVetoTurnPlayerId = match.CurrentVetoTurnPlayerId,
            Team1 = match.Players.Where(p => p.Team == 1)
                .Select(p => new VetoPlayerDto { PlayerId = p.PlayerId, Username = p.Player.Username }).ToList(),
            Team2 = match.Players.Where(p => p.Team == 2)
                .Select(p => new VetoPlayerDto { PlayerId = p.PlayerId, Username = p.Player.Username }).ToList(),
        };

        if (match.CurrentVetoTurnPlayerId.HasValue)
        {
            var turnPlayer = match.Players.FirstOrDefault(p => p.PlayerId == match.CurrentVetoTurnPlayerId.Value);
            dto.CurrentTurnTeam = turnPlayer?.Team ?? 0;
        }

        var allMapNames = new[] { "Mirage", "Inferno", "Dust2", "Overpass", "Nuke", "Vertigo", "Ancient" };
        foreach (var mapName in allMapNames)
        {
            if (match.AvailableMaps.Contains(mapName))
                dto.Maps.Add(new VetoMapDto { Name = mapName, IsBanned = false });
            else if (match.BannedMaps.Contains(mapName))
                dto.Maps.Add(new VetoMapDto { Name = mapName, IsBanned = true });
        }
        return dto;
    }
}