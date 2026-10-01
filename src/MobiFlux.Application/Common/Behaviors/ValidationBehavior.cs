using FluentValidation;
using MediatR;

namespace MobiFlux.Application;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any()) return await next(cancellationToken);
        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(validators.Select(validator => validator.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors).Where(failure => failure is not null).ToArray();
        if (failures.Length > 0) throw new ValidationException(failures);
        return await next(cancellationToken);
    }
}
