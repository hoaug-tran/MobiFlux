using Microsoft.EntityFrameworkCore;
using MobiFlux.Infrastructure.Persistence;
using MobiFlux.Service.Middleware;
using Serilog;

namespace MobiFlux.Service.Extensions;

public static class ApplicationBuilderExtensions
{
    public static async Task InitializeApiAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<MobiFluxDbContext>();
        await database.ApplyPendingMobiFluxSchemaMigrationsAsync();
    }

    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseSerilogRequestLogging(); app.UseMiddleware<CorrelationIdMiddleware>(); app.UseExceptionHandler();
        return app;
    }
}
