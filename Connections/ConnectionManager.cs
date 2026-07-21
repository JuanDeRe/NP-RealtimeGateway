using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace RealtimeGateway.Connections;

/// <summary>
/// Tracks active WebSocket connections grouped by match.
/// </summary>
public class ConnectionManager : IConnectionManager
{
    private readonly ConcurrentDictionary<string, PlayerConnection>
        _connections = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    public int ConnectedCount =>
        _connections.Values.Count(connection =>
            connection.WebSocket.State ==
            WebSocketState.Open
        );

    public PlayerConnection Add(
        string playerId,
        string matchId,
        WebSocket webSocket)
    {
        var connection = new PlayerConnection
        {
            ConnectionId = Guid.NewGuid().ToString("N"),
            PlayerId = playerId,
            MatchId = matchId,
            WebSocket = webSocket
        };

        _connections[connection.ConnectionId] = connection;

        return connection;
    }

    public void Remove(string connectionId)
    {
        if (_connections.TryRemove(
                connectionId,
                out var connection))
        {
            connection.SendLock.Dispose();
        }
    }

    public IReadOnlyCollection<PlayerConnection> GetByMatchId(
        string matchId)
    {
        return _connections.Values
            .Where(connection =>
                connection.MatchId == matchId &&
                connection.WebSocket.State == WebSocketState.Open
            )
            .ToList();
    }

    public async Task BroadcastToMatchAsync(
        string matchId,
        object payload,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(
            payload,
            JsonOptions
        );

        var bytes = Encoding.UTF8.GetBytes(json);

        var connections = GetByMatchId(matchId);

        var tasks = connections.Select(connection =>
            SendAsync(connection, bytes, cancellationToken)
        );

        await Task.WhenAll(tasks);
    }

    private static async Task SendAsync(
        PlayerConnection connection,
        byte[] bytes,
        CancellationToken cancellationToken)
    {
        await connection.SendLock.WaitAsync(cancellationToken);

        try
        {
            if (connection.WebSocket.State != WebSocketState.Open)
            {
                return;
            }

            await connection.WebSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                cancellationToken
            );
        }
        catch (WebSocketException)
        {
            // The disconnected socket will be removed by its handler.
        }
        finally
        {
            connection.SendLock.Release();
        }
    }
    public async Task SendToConnectionAsync(
        PlayerConnection connection,
        object payload,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(
            payload,
            JsonOptions
        );

        var bytes = Encoding.UTF8.GetBytes(json);

        await SendAsync(
            connection,
            bytes,
            cancellationToken
        );
    }
}