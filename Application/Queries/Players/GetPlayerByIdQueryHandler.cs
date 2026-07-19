using MediatR;
using MatchmakingEngine.Data;
using MatchmakingEngine.DTO;
using MatchmakingEngine.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace MatchmakingEngine.Application.Queries.Players;

public class GetPlayerByIdQueryHandler : IRequestHandler<GetPlayerByIdQuery, PlayerResponseDto>
{
    private readonly MatchmakingDbContext _context;

    public GetPlayerByIdQueryHandler(MatchmakingDbContext context)
    {
        _context = context;
    }

    public async Task<PlayerResponseDto> Handle(GetPlayerByIdQuery request, CancellationToken cancellationToken)
    {
        var player = await _context.Players
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (player == null)
            throw new NotFoundException($"Player with ID {request.Id} was not found");

        return new PlayerResponseDto(
            player.Id, player.Username, player.Mmr, player.TrustFactor, player.Region, player.Role, player.CreatedAt
            );
    }
}
