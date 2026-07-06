namespace MatchmakingEngine.Domain;

public record Match(
    Guid Id,
    Guid Player1Id,
    Guid Player2Id,
    double AverageMmr,
    DateTimeOffset CreatedAt
    );
