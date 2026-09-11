using System.ComponentModel.DataAnnotations;
using MatchmakingEngine.Domain;

namespace MatchmakingEngine.Application.DTO;

public record RegisterPlayerRequest(
    [Required][MaxLength(50)] string Username,
    [Required][MinLength(6)] string Password, 
    PlayerRegion Region
);
