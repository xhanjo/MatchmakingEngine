namespace MatchmakingEngine.Domain.Exceptions;

public class NotFoundException(string message) : DomainException(message);
