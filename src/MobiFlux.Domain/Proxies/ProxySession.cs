namespace MobiFlux.Domain.Proxies;

public sealed class ProxySession
{
    private ProxySession() { }

    public ProxySession(Guid id, Guid endpointId, Guid deviceId, string destinationHost, int destinationPort, DateTimeOffset startedAt, DateTimeOffset endedAt, long bytesUp, long bytesDown, string closeReason)
    {
        if (id == Guid.Empty) throw new ArgumentException("Session ID is required.", nameof(id));
        if (endpointId == Guid.Empty) throw new ArgumentException("Endpoint ID is required.", nameof(endpointId));
        if (deviceId == Guid.Empty) throw new ArgumentException("Device ID is required.", nameof(deviceId));
        if (string.IsNullOrWhiteSpace(destinationHost)) throw new ArgumentException("Destination host is required.", nameof(destinationHost));
        if (destinationPort is < 1 or > 65_535) throw new ArgumentOutOfRangeException(nameof(destinationPort));
        if (endedAt < startedAt) throw new ArgumentException("A session cannot end before it starts.", nameof(endedAt));
        if (bytesUp < 0 || bytesDown < 0) throw new ArgumentOutOfRangeException(nameof(bytesUp));
        if (string.IsNullOrWhiteSpace(closeReason)) throw new ArgumentException("Close reason is required.", nameof(closeReason));

        Id = id;
        EndpointId = endpointId;
        DeviceId = deviceId;
        DestinationHost = destinationHost.Trim();
        DestinationPort = destinationPort;
        StartedAt = startedAt;
        EndedAt = endedAt;
        BytesUp = bytesUp;
        BytesDown = bytesDown;
        CloseReason = closeReason.Trim();
    }

    public Guid Id { get; private set; }
    public Guid EndpointId { get; private set; }
    public Guid DeviceId { get; private set; }
    public string DestinationHost { get; private set; } = null!;
    public int DestinationPort { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset EndedAt { get; private set; }
    public long BytesUp { get; private set; }
    public long BytesDown { get; private set; }
    public string CloseReason { get; private set; } = null!;
    public long TotalBytes => BytesUp + BytesDown;
}
