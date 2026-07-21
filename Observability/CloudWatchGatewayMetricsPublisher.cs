using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Microsoft.Extensions.Options;
using RealtimeGateway.Connections;

namespace RealtimeGateway.Observability;

public sealed class CloudWatchGatewayMetricsPublisher
    : BackgroundService
{
    private const string ServiceName =
        "realtime-gateway";

    private readonly IConnectionManager
        _connectionManager;

    private readonly IGatewayMetrics _metrics;

    private readonly IAmazonCloudWatch
        _cloudWatch;

    private readonly GatewayMetricsOptions
        _options;

    private readonly ILogger<
        CloudWatchGatewayMetricsPublisher
    > _logger;

    public CloudWatchGatewayMetricsPublisher(
        IConnectionManager connectionManager,
        IGatewayMetrics metrics,
        IAmazonCloudWatch cloudWatch,
        IOptions<GatewayMetricsOptions> options,
        ILogger<
            CloudWatchGatewayMetricsPublisher
        > logger)
    {
        _connectionManager = connectionManager;
        _metrics = metrics;
        _cloudWatch = cloudWatch;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation(
                "CloudWatch gateway metrics are disabled."
            );

            return;
        }

        var interval =
            _options.PublishIntervalSeconds <= 0
                ? 60
                : _options.PublishIntervalSeconds;

        _logger.LogInformation(
            "CloudWatch RealtimeGateway metrics " +
            "publisher started. Namespace={Namespace}, " +
            "Environment={Environment}, " +
            "IntervalSeconds={IntervalSeconds}",
            _options.Namespace,
            _options.Environment,
            interval
        );

        await PublishAsync(stoppingToken);

        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(interval)
        );

        try
        {
            while (await timer.WaitForNextTickAsync(
                       stoppingToken))
            {
                await PublishAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "CloudWatch RealtimeGateway metrics " +
                "publisher stopped."
            );
        }
    }

    private async Task PublishAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var snapshot =
                _metrics.TakeSnapshot();

            var timestamp = DateTime.UtcNow;

            var metricData = new List<MetricDatum>
            {
                CreateMetric(
                    "ConnectedWebSockets",
                    _connectionManager.ConnectedCount,
                    StandardUnit.Count,
                    timestamp
                ),
                CreateMetric(
                    "RoutingFailures",
                    snapshot.RoutingFailures,
                    StandardUnit.Count,
                    timestamp
                ),
                CreateMetric(
                    "GameServiceRequestFailures",
                    snapshot.GameServiceRequestFailures,
                    StandardUnit.Count,
                    timestamp
                )
            };

            await _cloudWatch.PutMetricDataAsync(
                new PutMetricDataRequest
                {
                    Namespace = _options.Namespace,
                    MetricData = metricData
                },
                cancellationToken
            );

            _logger.LogInformation(
                "Published RealtimeGateway metrics. " +
                "ConnectedWebSockets={Connections}, " +
                "RoutingFailures={RoutingFailures}, " +
                "GameServiceRequestFailures=" +
                "{RequestFailures}",
                _connectionManager.ConnectedCount,
                snapshot.RoutingFailures,
                snapshot.GameServiceRequestFailures
            );
        }
        catch (OperationCanceledException)
            when (cancellationToken
                .IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to publish RealtimeGateway " +
                "metrics to CloudWatch."
            );
        }
    }

    private MetricDatum CreateMetric(
        string name,
        double value,
        StandardUnit unit,
        DateTime timestamp)
    {
        return new MetricDatum
        {
            MetricName = name,
            Value = value,
            Unit = unit,
            Timestamp = timestamp,
            Dimensions =
            [
                new Dimension
                {
                    Name = "Environment",
                    Value = _options.Environment
                },
                new Dimension
                {
                    Name = "Service",
                    Value = ServiceName
                }
            ]
        };
    }
}