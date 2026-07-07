using MatchmakingEngine.Domain;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace MatchmakingEngine.Services;

public class MatchmakingQueue : IMatchmakingQueue
{
    private readonly Channel<MatchmakingTicket> _queue;
    private readonly ConcurrentDictionary<Guid, MatchmakingTicket> _activePlayers = new();

    public MatchmakingQueue()
    {
        _queue = Channel.CreateUnbounded<MatchmakingTicket>();
    }
    public async ValueTask<MatchmakingTicket?> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }

    public async ValueTask EnqueueAsync(MatchmakingTicket ticket)
    {
        await _queue.Writer.WriteAsync(ticket);
        _activePlayers.TryAdd(ticket.PlayerId, ticket);
    }

    public bool IsPlayerInQueue(Guid playerId)
    {
        return _activePlayers.ContainsKey(playerId);
    }

    public void RemovePlayer(Guid playerId)
    {
        _activePlayers.TryRemove(playerId, out _);
    }
}
