using MobiFlux.Application;
using MobiFlux.Domain;
using MobiFlux.Infrastructure;
using MobiFlux.Proxy;
using MobiFlux.Service.Configuration;
using MobiFlux.Service.Extensions;
using MobiFlux.Service.Services;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();
try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Configuration.AddMobiFluxEnvironment(builder.Environment);
    builder.Host.UseWindowsService().UseSerilog((context, _, logger) => logger.ReadFrom.Configuration(context.Configuration).Enrich.FromLogContext());
    builder.Services.AddDomain();
    builder.Services.AddApplication();
    builder.Services.AddProxy();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApiServices(builder.Configuration);

    var app = builder.Build();
    await app.InitializeApiAsync();
    app.UseApiPipeline();
    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.MapHealthChecks("/health"); app.MapControllers(); app.MapHub<RuntimeHub>("/hubs/runtime");
    foreach (var path in new[] { "/", "/devices", "/proxies", "/pools", "/rotation", "/sessions", "/networking", "/settings" })
        app.MapGet(path, async context =>
        {
            var file = app.Environment.WebRootFileProvider.GetFileInfo("index.html");
            if (!file.Exists) { context.Response.StatusCode = StatusCodes.Status404NotFound; return; }
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.Headers.CacheControl = "no-cache";
            await context.Response.SendFileAsync(file, context.RequestAborted);
        });
    await app.RunAsync();
}
catch (Exception exception)
{
    Log.Fatal(exception, "MobiFlux API terminated unexpectedly");
}
finally { await Log.CloseAndFlushAsync(); }
