using System.Net.WebSockets;

namespace RealtimeGateway.Connections;

public interface IConnectionManager
{
    PlayerConnection Add(
        string playerId,
        string matchId,
        WebSocket webSocket
    );

    void Remove(string connectionId);

    IReadOnlyCollection<PlayerConnection> GetByMatchId(
        string matchId
    );

    Task BroadcastToMatchAsync(
        string matchId,
        object payload,
        CancellationToken cancellationToken = default
    );
    Task SendToConnectionAsync(
        PlayerConnection connection,
        object payload,
        CancellationToken cancellationToken = default
    );
}