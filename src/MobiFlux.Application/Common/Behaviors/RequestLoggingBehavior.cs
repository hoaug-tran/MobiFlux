using MediatR;
using Microsoft.Extensions.Logging;

namespace MobiFlux.Application;

public sealed class RequestLoggingBehavior<TRequest, TResponse>(ILogger<RequestLoggingBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogDebug("Handling {RequestName}", requestName);
        var response = await next(cancellationToken);
        logger.LogDebug("Handled {RequestName}", requestName);
        return response;
    }
}
