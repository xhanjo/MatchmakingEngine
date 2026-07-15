using MatchmakingEngine.Domain;
using System.ComponentModel.DataAnnotations;

namespace MatchmakingEngine.DTO;

public record RegisterPlayerRequest(
    [Required][MaxLength(50)] string Username,
    [Required][MinLength(6)] string Password, 
    PlayerRegion Region
);
