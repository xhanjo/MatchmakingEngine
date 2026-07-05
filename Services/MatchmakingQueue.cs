using MatchmakingEngine.Domain;
using System.Threading.Channels;

namespace MatchmakingEngine.Services;

public class MatchmakingQueue : IMatchmakingQueue
{
    private readonly Channel<MatchmakingTicket> _queue;

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
    }
}
