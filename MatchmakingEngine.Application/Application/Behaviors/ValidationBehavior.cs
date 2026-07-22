using FluentValidation;
using MediatR;
using ValidationException = MatchmakingEngine.Domain.Exceptions.ValidationException;

namespace MatchmakingEngine.Application.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
        {
            var errorsDictionary = failures
                .GroupBy(x => x.PropertyName)
                .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.ErrorMessage).ToArray()
                );

            throw new ValidationException(errorsDictionary);
        }

        return await next();
    }
}
