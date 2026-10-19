using Serilog.Context;

namespace MobiFlux.Service.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var supplied = context.Request.Headers["X-Correlation-ID"].ToString();
        var correlationId = supplied.Length is > 0 and <= 64 && supplied.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_')
            ? supplied : Guid.NewGuid().ToString("N");
        context.TraceIdentifier = correlationId; context.Response.Headers["X-Correlation-ID"] = correlationId;
        using (LogContext.PushProperty("CorrelationId", correlationId)) await next(context);
    }
}
