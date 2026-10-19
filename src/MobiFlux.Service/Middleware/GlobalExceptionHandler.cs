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
            System.Text.Json.JsonException => (StatusCodes.Status400BadRequest, new ApiError("InvalidJson", "Nội dung yêu cầu không đúng định dạng JSON.")),
            BadHttpRequestException badRequest => (badRequest.StatusCode, new ApiError("InvalidRequest", "Yêu cầu không hợp lệ hoặc vượt giới hạn cho phép.")),
            TimeoutException => (StatusCodes.Status504GatewayTimeout, new ApiError("RequestTimeout", "Dịch vụ xử lý quá lâu. Kiểm tra kết nối rồi thử lại.")),
            ValidationException validation => (StatusCodes.Status400BadRequest, new ApiError("ValidationFailed", "Thông tin chưa hợp lệ. Kiểm tra các trường được đánh dấu.", validation.Errors.GroupBy(error => error.PropertyName).ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray()))),
            KeyNotFoundException => (StatusCodes.Status404NotFound, new ApiError("NotFound", "Không tìm thấy dữ liệu yêu cầu. Làm mới trang rồi thử lại.")),
            InvalidOperationException invalid => (StatusCodes.Status409Conflict, new ApiError("InvalidState", invalid.Message)),
            ArgumentException argument => (StatusCodes.Status400BadRequest, new ApiError("InvalidArgument", argument.Message)),
            _ => (StatusCodes.Status500InternalServerError, new ApiError("UnexpectedError", "Service gặp lỗi ngoài dự kiến. Thử lại; nếu lỗi tiếp diễn, cung cấp mã tra cứu để kiểm tra."))
        };
        if (status >= 500) logger.LogError(exception, "Request failed with {ErrorCode} for correlation ID {CorrelationId}", error.Code, context.TraceIdentifier);
        else logger.LogWarning("Request rejected with {ErrorCode} for correlation ID {CorrelationId}", error.Code, context.TraceIdentifier);
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(error, context.TraceIdentifier), cancellationToken);
        return true;
    }
}
