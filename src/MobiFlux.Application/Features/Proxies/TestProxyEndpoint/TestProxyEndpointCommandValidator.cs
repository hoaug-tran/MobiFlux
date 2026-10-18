using System.Net;
using FluentValidation;
using MobiFlux.Domain.Devices;

namespace MobiFlux.Application.Features.Proxies.TestProxyEndpoint;

public sealed class TestProxyEndpointCommandValidator : AbstractValidator<TestProxyEndpointCommand>
{
    public TestProxyEndpointCommandValidator()
    {
        When(command => command.EndpointId is null, () =>
        {
            RuleFor(command => command.BindAddress).Must(address => IPAddress.TryParse(address, out var ip) && IPAddress.IsLoopback(ip)).WithMessage("Proxy test requires a loopback address.");
            RuleFor(command => command.Port).InclusiveBetween(1, 65535);
            RuleFor(command => command.Protocol).Must(protocol => Enum.TryParse<ProxyProtocol>(protocol, true, out var parsed) && Enum.IsDefined(parsed));
        });
    }
}
