using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RealtimeGateway.Configuration;
using RealtimeGateway.Models;
using StackExchange.Redis;

namespace RealtimeGateway.Routing;

/// <summary>
/// Resolves the GameService instance that owns a match.
/// </summary>
public class RedisMatchRouteResolver
    : IMatchRouteResolver
{
    private static readonly JsonSerializerOptions
        JsonOptions = new()
        {
            PropertyNamingPolicy =
                JsonNamingPolicy.CamelCase,

            PropertyNameCaseInsensitive = true
        };

    private static readonly TimeSpan CacheDuration =
        TimeSpan.FromSeconds(60);

    private readonly ConcurrentDictionary<
        string,
        CachedRoute
    > _cache = new();

    private readonly IConnectionMultiplexer _redis;
    private readonly RedisOptions _options;

    public RedisMatchRouteResolver(
        IConnectionMultiplexer redis,
        IOptions<RedisOptions> options)
    {
        _redis = redis;
        _options = options.Value;
    }

    public MatchRoute? GetRoute(string matchId)
    {
        if (string.IsNullOrWhiteSpace(matchId))
        {
            return null;
        }

        if (_cache.TryGetValue(
                matchId,
                out var cachedRoute))
        {
            if (cachedRoute.ExpiresAtUtc >
                DateTime.UtcNow)
            {
                return cachedRoute.Route;
            }

            _cache.TryRemove(
                matchId,
                out _
            );
        }

        var database = _redis.GetDatabase();

        var json = database.StringGet(
            MatchRouteKey(matchId)
        );

        if (!json.HasValue)
        {
            return null;
        }

        try
        {
            var route =
                JsonSerializer.Deserialize<MatchRoute>(
                    json.ToString(),
                    JsonOptions
                );

            if (route is null)
            {
                return null;
            }

            _cache[matchId] =
                new CachedRoute(
                    route,
                    DateTime.UtcNow.Add(
                        CacheDuration
                    )
                );

            return route;
        }
        catch (JsonException)
        {
            database.KeyDelete(
                MatchRouteKey(matchId)
            );

            _cache.TryRemove(
                matchId,
                out _
            );

            return null;
        }
    }

    public string? GetGameServiceUrl(
        string matchId)
    {
        var route = GetRoute(matchId);

        if (route is null ||
            string.IsNullOrWhiteSpace(
                route.InternalUrl))
        {
            return null;
        }

        return route.InternalUrl.TrimEnd('/');
    }

    public void RemoveRoute(string matchId)
    {
        if (string.IsNullOrWhiteSpace(matchId))
        {
            return;
        }

        _cache.TryRemove(
            matchId,
            out _
        );

        _redis.GetDatabase().KeyDelete(
            MatchRouteKey(matchId)
        );
    }

    private string MatchRouteKey(string matchId)
    {
        return $"{_options.InstanceName}" +
               $"routing:match:{matchId}";
    }

    private sealed record CachedRoute(
        MatchRoute Route,
        DateTime ExpiresAtUtc
    );
}