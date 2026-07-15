using System.Text.Json;
using Microsoft.Extensions.Options;
using RealtimeGateway.Configuration;
using RealtimeGateway.Connections;
using RealtimeGateway.Contracts;
using RealtimeGateway.Routing;
using StackExchange.Redis;

namespace RealtimeGateway.BackgroundServices;

/// <summary>
/// Reads durable match events and broadcasts them to local WebSocket clients.
/// </summary>
public class MatchEventStreamProcessor
    : BackgroundService
{
    private const string StreamKey =
        "nightwatch:streams:match-events";

    private readonly IConnectionMultiplexer _redis;
    private readonly IConnectionManager _connectionManager;
    private readonly RedisOptions _options;
    private readonly IMatchRouteResolver _matchRouteResolver;
    private readonly ILogger<MatchEventStreamProcessor>
        _logger;

    public MatchEventStreamProcessor(
        IConnectionMultiplexer redis,
        IConnectionManager connectionManager,
        IMatchRouteResolver matchRouteResolver,
        IOptions<RedisOptions> options,
        ILogger<MatchEventStreamProcessor> logger)
    {
        _redis = redis;
        _connectionManager = connectionManager;
        _matchRouteResolver = matchRouteResolver;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var database = _redis.GetDatabase();

        var cursorKey =
            $"{_options.InstanceName}" +
            $"consumers:gateway:" +
            $"{_options.ConsumerId}:last-event-id";

        var storedCursor = database.StringGet(
            cursorKey
        );

        var lastEventId = storedCursor.HasValue
            ? storedCursor.ToString()
            : "0-0";

        _logger.LogInformation(
            "Gateway stream processor {ConsumerId} " +
            "started at event {EventId}",
            _options.ConsumerId,
            lastEventId
        );

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var entries = database.StreamRead(
                    StreamKey,
                    lastEventId,
                    count: 20
                );

                foreach (var entry in entries)
                {
                    await ProcessEntryAsync(
                        entry,
                        stoppingToken
                    );

                    lastEventId = entry.Id.ToString();

                    database.StringSet(
                        cursorKey,
                        lastEventId
                    );
                }
            }
            catch (RedisConnectionException exception)
            {
                _logger.LogError(
                    exception,
                    "Redis connection failed while " +
                    "reading Gateway match events"
                );
            }
            catch (RedisTimeoutException exception)
            {
                _logger.LogError(
                    exception,
                    "Redis timed out while reading " +
                    "Gateway match events"
                );
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Unexpected Gateway stream error"
                );
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromMilliseconds(500),
                    stoppingToken
                );
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task ProcessEntryAsync(
        StreamEntry entry,
        CancellationToken cancellationToken)
    {
        var eventType = GetValue(
            entry,
            "eventType"
        );

        if (eventType != "MatchFinished")
        {
            return;
        }

        var payload = GetValue(
            entry,
            "payload"
        );

        if (string.IsNullOrWhiteSpace(payload))
        {
            return;
        }

        var matchFinishedEvent =
            JsonSerializer.Deserialize<
                MatchFinishedEvent
            >(payload);

        if (matchFinishedEvent is null)
        {
            return;
        }

        await _connectionManager.BroadcastToMatchAsync(
            matchFinishedEvent.MatchId,
            matchFinishedEvent,
            cancellationToken
        );
        _matchRouteResolver.RemoveRoute(
            matchFinishedEvent.MatchId);
        _logger.LogInformation(
            "Broadcast MatchFinished for match " +
            "{MatchId} to local WebSocket clients",
            matchFinishedEvent.MatchId
        );
    }

    private static string? GetValue(
        StreamEntry entry,
        string name)
    {
        foreach (var pair in entry.Values)
        {
            if (pair.Name == name)
            {
                return pair.Value.ToString();
            }
        }

        return null;
    }
}