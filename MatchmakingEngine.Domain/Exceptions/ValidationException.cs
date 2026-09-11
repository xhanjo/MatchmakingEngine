namespace MatchmakingEngine.Domain.Exceptions;

public class ValidationException(IReadOnlyDictionary<string, string[]> errors)
    : DomainException("One or more validation errors occured.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
