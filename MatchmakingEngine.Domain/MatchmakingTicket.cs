namespace MatchmakingEngine.Domain;


public record MatchmakingTicket
    (
    Guid TicketId,
    Guid PlayerId,
    string Username,
    int Mmr,
    double TrustFactor,
    PlayerRegion Region,
    DateTimeOffset EnqueuedAt,
    GameMode GameMode,
    Guid? PartyId
    );
