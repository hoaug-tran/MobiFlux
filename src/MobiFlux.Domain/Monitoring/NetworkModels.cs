namespace MobiFlux.Domain.Monitoring;

public sealed class NetworkSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeviceId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string? Carrier { get; set; }
    public string? LocalIpv4 { get; set; }
    public string? LocalIpv6 { get; set; }
    public string? EgressIpv4 { get; set; }
    public string? EgressIpv6 { get; set; }
    public int? SignalDbm { get; set; }
    public int? LatencyMs { get; set; }
    public decimal? PacketLoss { get; set; }
}

public readonly record struct MobiFluxError(string Code, string Message, string? TechnicalDetail, Guid? DeviceId, Guid OperationId, bool Retryable, DateTimeOffset Timestamp);
