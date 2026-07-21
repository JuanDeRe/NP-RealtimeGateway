namespace RealtimeGateway.Observability;

public sealed class GatewayMetricsOptions
{
    public const string SectionName = "Metrics";

    public bool Enabled { get; set; } = true;

    public string Namespace { get; set; } =
        "NightwatchProtocol";

    public string Environment { get; set; } =
        "dev";

    public int PublishIntervalSeconds { get; set; } =
        60;
}