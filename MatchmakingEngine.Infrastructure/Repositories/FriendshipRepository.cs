using MatchmakingEngine.Application.Interfaces.Repositories;
using MatchmakingEngine.Data;
using MatchmakingEngine.Domain;
using Microsoft.EntityFrameworkCore;

namespace MatchmakingEngine.Infrastructure.Repositories;

public class FriendshipRepository : IFriendshipRepository
{
    private readonly MatchmakingDbContext _dbContext;

    public FriendshipRepository(MatchmakingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Friendship?> GetBetweenPlayersAsync(Guid player1Id, Guid player2Id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Friendships
            .FirstOrDefaultAsync(f =>
            (f.SenderId == player1Id && f.ReceiverId == player2Id) ||
            (f.SenderId == player2Id && f.ReceiverId == player1Id), cancellationToken);
    }

    public async Task<Friendship?> GetByIdAsync(Guid id, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = trackChanges ? _dbContext.Friendships : _dbContext.Friendships.AsNoTracking();
        return await query.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Friendship>> GetFriendsAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Friendships
            .AsNoTracking()
            .Include(f => f.Sender)
            .Include(f => f.Receiver)
            .Where(f => f.Status == FriendshipStatus.Accepted &&
                        (f.SenderId == playerId || f.ReceiverId == playerId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Friendship>> GetPendingRequestsAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Friendships
            .AsNoTracking()
            .Include(f => f.Sender)
            .Where(f => f.ReceiverId == playerId && f.Status == FriendshipStatus.Pending)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Friendship friendship, CancellationToken cancellationToken = default)
    {
        await _dbContext.Friendships.AddAsync(friendship, cancellationToken);
    }

    public void Update(Friendship friendship)
    {
        _dbContext.Friendships.Update(friendship);
    }
    public void Delete(Friendship friendship)
    {
        _dbContext.Friendships.Remove(friendship);
    }

}
