namespace RealtimeGateway.Observability;

public sealed record GatewayMetricsSnapshot(
    long RoutingFailures,
    long GameServiceRequestFailures
);