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
    app.MapHealthChecks("/health"); app.MapControllers(); app.MapHub<RuntimeHub>("/hubs/runtime");
    await app.RunAsync();
}
catch (Exception exception)
{
    Log.Fatal(exception, "MobiFlux API terminated unexpectedly");
}
finally { await Log.CloseAndFlushAsync(); }
