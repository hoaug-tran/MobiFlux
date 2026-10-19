using MobiFlux.Service.Configuration;
using MobiFlux.Service.Middleware;
using MobiFlux.Service.Services;
using MobiFlux.Shared.Configuration;

namespace MobiFlux.Service.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<StorageOptions>().BindConfiguration(StorageOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<AdbOptions>().BindConfiguration(AdbOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<ProxyOptions>().BindConfiguration(ProxyOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<RoutingOptions>().BindConfiguration(RoutingOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<SessionPersistenceOptions>().BindConfiguration(SessionPersistenceOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<ApiOptions>().BindConfiguration(ApiOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<TelemetryOptions>().BindConfiguration(TelemetryOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
        services.AddSingleton(TimeProvider.System);
        services.AddHostedService<RuntimeReconciliationWorker>();
        services.AddHostedService<ProxySessionPersistenceWorker>();
        services.AddHostedService<RuntimeTelemetryWorker>();
        services.AddControllers().ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = context =>
            new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(MobiFlux.Shared.Common.ApiResponse<object>.Fail(
                new MobiFlux.Shared.Common.ApiError("ValidationFailed", "Thông tin yêu cầu chưa hợp lệ. Kiểm tra dữ liệu và thử lại.",
                    context.ModelState.Where(entry => entry.Value?.Errors.Count > 0).ToDictionary(entry => entry.Key, entry => entry.Value!.Errors.Select(_ => "Giá trị không đúng định dạng hoặc thiếu trường bắt buộc.").Distinct().ToArray())), context.HttpContext.TraceIdentifier)));
        services.AddSignalR(); services.AddHealthChecks(); services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        return services;
    }
}
