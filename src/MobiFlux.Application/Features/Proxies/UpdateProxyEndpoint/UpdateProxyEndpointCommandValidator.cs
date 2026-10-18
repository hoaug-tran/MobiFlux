using FluentValidation;
namespace MobiFlux.Application.Features.Proxies.UpdateProxyEndpoint;
public sealed class UpdateProxyEndpointCommandValidator : AbstractValidator<UpdateProxyEndpointCommand>
{
    public UpdateProxyEndpointCommandValidator()
    {
        RuleFor(command => command.EndpointId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(128);
        RuleFor(command => command.MaxConnections).InclusiveBetween(1, 65535);
    }
}
