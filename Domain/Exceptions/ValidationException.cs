namespace MatchmakingEngine.Domain.Exceptions;

public class ValidationException : DomainException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }
    public ValidationException(IReadOnlyDictionary<string, string[]> errors) : base ("One or more validation errors occured.")
    {
        Errors = errors;
    }
}
