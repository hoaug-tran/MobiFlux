using System.Net;
using MobiFlux.Shared.Common;

namespace MobiFlux.Service.Middleware;

public sealed class LocalManagementMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var remote = context.Connection.RemoteIpAddress;
        var host = context.Request.Host.Host;
        var localHost = string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) || IPAddress.TryParse(host, out var address) && IPAddress.IsLoopback(address);
        var origin = context.Request.Headers.Origin.ToString();
        var sameOrigin = string.IsNullOrEmpty(origin) || Uri.TryCreate(origin, UriKind.Absolute, out var uri) && string.Equals(uri.Authority, context.Request.Host.Value, StringComparison.OrdinalIgnoreCase) && string.Equals(uri.Scheme, context.Request.Scheme, StringComparison.OrdinalIgnoreCase);
        var crossSite = string.Equals(context.Request.Headers["Sec-Fetch-Site"], "cross-site", StringComparison.OrdinalIgnoreCase);
        if (remote is null || !IPAddress.IsLoopback(remote) || !localHost || !sameOrigin || crossSite)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(new ApiError("LocalManagementOnly", "Quản trị chỉ cho phép trên máy chạy service và cùng nguồn truy cập."), context.TraceIdentifier), context.RequestAborted);
            return;
        }
        await next(context);
    }
}
