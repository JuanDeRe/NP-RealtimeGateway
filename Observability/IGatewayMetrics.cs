namespace RealtimeGateway.Observability;

public interface IGatewayMetrics
{
    void RecordRoutingFailure();

    void RecordGameServiceRequestFailure();

    GatewayMetricsSnapshot TakeSnapshot();
}