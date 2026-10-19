using FluentValidation;
using MobiFlux.Domain.Devices;

namespace MobiFlux.Application.Features.Proxies.UpdateProxyEndpoint;

public sealed class UpdateProxyEndpointCommandValidator : AbstractValidator<UpdateProxyEndpointCommand>
{
    public UpdateProxyEndpointCommandValidator()
    {
        RuleFor(command => command.EndpointId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(128);
        RuleFor(command => command.BindAddress).NotEmpty();
        RuleFor(command => command.Port).InclusiveBetween(1, 65535);
        RuleFor(command => command.Protocol).Must(protocol => Enum.TryParse<ProxyProtocol>(protocol, true, out var value) && Enum.IsDefined(value)).WithMessage("Protocol must be SOCKS5 or HTTP CONNECT.");
        RuleFor(command => command.MaxConnections).InclusiveBetween(1, 65535);
        RuleFor(command => command).Must(command => (command.DeviceId is null) ^ (command.PoolId is null)).WithMessage("Proxy must target either a device or a pool.");
    }
}
