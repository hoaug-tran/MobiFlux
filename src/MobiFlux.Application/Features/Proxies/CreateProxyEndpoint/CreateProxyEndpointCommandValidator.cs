using FluentValidation;
using System.Net;
using MobiFlux.Domain.Devices;

namespace MobiFlux.Application.Features.Proxies.CreateProxyEndpoint;

public sealed class CreateProxyEndpointCommandValidator : AbstractValidator<CreateProxyEndpointCommand>
{
    public CreateProxyEndpointCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(128);
        RuleFor(command => command.BindAddress).Must(address => IPAddress.TryParse(address, out _)).WithMessage("Bind address must be an IP address.");
        RuleFor(command => command.Port).InclusiveBetween(1, 65535);
        RuleFor(command => command.Protocol).Must(protocol => Enum.TryParse<ProxyProtocol>(protocol, true, out _)).WithMessage("Protocol must be SOCKS5 or HTTP CONNECT.");
        RuleFor(command => command).Must(command => (command.DeviceId is null) != (command.PoolId is null)).WithMessage("Specify exactly one device or pool target.");
    }
}
