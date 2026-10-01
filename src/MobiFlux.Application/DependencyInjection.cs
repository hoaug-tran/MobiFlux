using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace MobiFlux.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssemblyContaining<AssemblyMarker>();
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
            configuration.AddOpenBehavior(typeof(RequestLoggingBehavior<,>));
        });
        services.AddValidatorsFromAssemblyContaining<AssemblyMarker>();
        return services;
    }
}
