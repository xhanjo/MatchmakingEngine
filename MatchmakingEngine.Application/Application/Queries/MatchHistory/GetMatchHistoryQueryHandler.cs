using MatchmakingEngine.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace MatchmakingEngine.Application.Application.Queries.MatchHistory;

public class GetMatchHistoryQuery : IRequest<List<MatchDto>>
{
    public Guid PlayerId { get; }
    public GetMatchHistoryQuery(Guid playerID) => PlayerId = playerID;
}

public class GetMatchHistoryQueryHandler : IRequestHandler<GetMatchHistoryQuery, List<MatchDto>>
{
    private readonly IMatchmakingDbContext _context;

    public GetMatchHistoryQueryHandler(IMatchmakingDbContext context)
    {
        _context = context;
    }
    public async Task<List<MatchDto>> Handle(GetMatchHistoryQuery request, CancellationToken cancellationToken)
    {
        var matches = await _context.Matches
            .AsNoTracking()
            .Include(m => m.Player1)
            .Include(m => m.Player2)
            .Where(m => m.Player1Id == request.PlayerId || m.Player2Id == request.PlayerId)
            .ToListAsync(cancellationToken);

        var dtos = matches.Select(m => new MatchDto(
                m.Id,
                m.Status.ToString(),
                new PlayerDto(m.Player1Id, m.Player1.Username, m.Player1.Mmr),
                new PlayerDto(m.Player2Id, m.Player2.Username, m.Player2.Mmr)
            )).ToList();

        return dtos;
    }
}
