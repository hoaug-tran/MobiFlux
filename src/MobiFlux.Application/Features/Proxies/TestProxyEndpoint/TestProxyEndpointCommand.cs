using MediatR;
using MobiFlux.Shared.Contracts;

namespace MobiFlux.Application.Features.Proxies.TestProxyEndpoint;

public sealed record TestProxyEndpointCommand(Guid? EndpointId, string BindAddress = "127.0.0.1", int Port = 1080, string Protocol = "Socks5", string? Username = null, string? Password = null) : IRequest<ProxyTestResultDto>;
