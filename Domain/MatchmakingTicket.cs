namespace MatchmakingEngine.Domain;


public record MatchmakingTicket
    (
    Guid TicketId,
    Guid PlayerId,
    string Username,
    double Mmr,
    double TrustFactor,
    PlayerRegion Region,
    DateTimeOffset EnqueuedAt
    );
