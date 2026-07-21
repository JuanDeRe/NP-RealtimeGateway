namespace RealtimeGateway.Observability;

public sealed class GatewayMetrics : IGatewayMetrics
{
    private long _routingFailures;
    private long _gameServiceRequestFailures;

    public void RecordRoutingFailure()
    {
        Interlocked.Increment(
            ref _routingFailures
        );
    }

    public void RecordGameServiceRequestFailure()
    {
        Interlocked.Increment(
            ref _gameServiceRequestFailures
        );
    }

    public GatewayMetricsSnapshot TakeSnapshot()
    {
        return new GatewayMetricsSnapshot(
            Interlocked.Exchange(
                ref _routingFailures,
                0
            ),
            Interlocked.Exchange(
                ref _gameServiceRequestFailures,
                0
            )
        );
    }
}