using RealtimeGateway.Models;

namespace RealtimeGateway.Routing;

public interface IMatchRouteResolver
{
    MatchRoute? GetRoute(string matchId);

    string? GetGameServiceUrl(string matchId);

    void RemoveRoute(string matchId);
}