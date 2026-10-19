using MediatR;
using MobiFlux.Application.Abstractions;

namespace MobiFlux.Application.Features.Proxies.DeleteProxyPool;

public sealed record DeleteProxyPoolCommand(Guid PoolId) : IRequest;

public sealed class DeleteProxyPoolHandler(IProxyRepository proxies, IDeviceRepository devices, IProxyRuntime runtime) : IRequestHandler<DeleteProxyPoolCommand>
{
    public async Task Handle(DeleteProxyPoolCommand command, CancellationToken cancellationToken)
    {
        var pool = await proxies.GetPoolAsync(command.PoolId, cancellationToken) ?? throw new KeyNotFoundException("Không tìm thấy pool.");
        if ((await proxies.ListEndpointsAsync(cancellationToken)).Any(endpoint => endpoint.PoolId == pool.Id))
            throw new InvalidOperationException("Pool đang được proxy sử dụng. Xóa các proxy liên quan trước khi xóa pool.");
        proxies.RemovePool(pool);
        await proxies.SaveChangesAsync(cancellationToken);
        runtime.RefreshRoutes(await devices.ListAsync(cancellationToken), await proxies.ListPoolsAsync(cancellationToken));
    }
}
