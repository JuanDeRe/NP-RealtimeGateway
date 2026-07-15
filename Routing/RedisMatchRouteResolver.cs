using System.Text.Json;
using RealtimeGateway.Configuration;
using RealtimeGateway.Models;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using RealtimeGateway.Routing;

namespace RealtimeGateway.Routing;

/// <summary>
/// Resolves the GameService instance that owns a match.
/// </summary>
public class RedisMatchRouteResolver
    : IMatchRouteResolver
{
    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            PropertyNamingPolicy =
                JsonNamingPolicy.CamelCase,

            PropertyNameCaseInsensitive = true
        };

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
            return JsonSerializer.Deserialize<MatchRoute>(
                json.ToString(),
                JsonOptions
            );
        }
        catch (JsonException)
        {
            database.KeyDelete(
                MatchRouteKey(matchId)
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

        _redis.GetDatabase().KeyDelete(
            MatchRouteKey(matchId)
        );
    }

    private string MatchRouteKey(string matchId)
    {
        return $"{_options.InstanceName}" +
               $"routing:match:{matchId}";
    }
}