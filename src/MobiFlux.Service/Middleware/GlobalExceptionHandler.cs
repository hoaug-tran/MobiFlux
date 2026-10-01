using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using MobiFlux.Shared.Common;

namespace MobiFlux.Service.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, error) = exception switch
        {
            ValidationException validation => (StatusCodes.Status400BadRequest, new ApiError("ValidationFailed", "One or more validation errors occurred.", validation.Errors.GroupBy(error => error.PropertyName).ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray()))),
            KeyNotFoundException => (StatusCodes.Status404NotFound, new ApiError("NotFound", "The requested resource was not found.")),
            ArgumentException argument => (StatusCodes.Status400BadRequest, new ApiError("InvalidArgument", argument.Message)),
            _ => (StatusCodes.Status500InternalServerError, new ApiError("UnexpectedError", "An unexpected error occurred. Use the correlation ID when contacting support."))
        };
        logger.LogError(exception, "Request failed with {ErrorCode} for correlation ID {CorrelationId}", error.Code, context.TraceIdentifier);
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(error, context.TraceIdentifier), cancellationToken);
        return true;
    }
}
