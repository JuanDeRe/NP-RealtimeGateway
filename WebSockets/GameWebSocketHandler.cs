using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using RealtimeGateway.Connections;
using RealtimeGateway.Contracts;
using RealtimeGateway.Services;
using RealtimeGateway.Security;

namespace RealtimeGateway.WebSockets;

public class GameWebSocketHandler
{
    private readonly IGameServiceClient _gameServiceClient;
    private readonly IConnectionManager _connectionManager;

    public GameWebSocketHandler(
        IGameServiceClient gameServiceClient,
        IConnectionManager connectionManager)
    {
        _gameServiceClient = gameServiceClient;
        _connectionManager = connectionManager;
    }

    public async Task HandleAsync(
        WebSocket webSocket,
        AuthenticatedConnection connection,
        string matchId)
    {
        var buffer = new byte[4096];

        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer),
                CancellationToken.None
            );

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Connection closed by client",
                    CancellationToken.None
                );

                break;
            }

            var json = Encoding.UTF8.GetString(buffer, 0, result.Count);

            var baseMessage = JsonSerializer.Deserialize<ClientGameMessage>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }
            );

            if (baseMessage is null)
            {
                await SendErrorAsync(webSocket, "Invalid message format");
                continue;
            }

            if (baseMessage.Type == "MoveCommand")
            {
                await HandleMoveCommandAsync(
                    webSocket,
                    json,
                    connection,
                    matchId
                );                
                continue;
            }

            await SendErrorAsync(webSocket, $"Unsupported message type: {baseMessage.Type}");
        }
    }

    private async Task HandleMoveCommandAsync(
        WebSocket webSocket,
        string json,
        AuthenticatedConnection connection,
        string connectionMatchId)
    {
        var command = JsonSerializer.Deserialize<MoveCommandMessage>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }
        );

        if (command is null)
        {
            await SendErrorAsync(
                webSocket,
                "Invalid MoveCommand payload"
            );

            return;
        }

        if (command.MatchId != connectionMatchId)
        {
            await SendErrorAsync(
                webSocket,
                "Command match does not match the active connection"
            );

            return;
        }

        try
        {
            var movementEvent = await _gameServiceClient.MoveAsync(
                command,
                connection.PlayerId
            );

            if (movementEvent is null)
            {
                await SendErrorAsync(
                    webSocket,
                    "GameService rejected movement"
                );

                return;
            }

            await _connectionManager.BroadcastToMatchAsync(
                connectionMatchId,
                movementEvent
            );
        }
        catch (InvalidOperationException exception)
        {
            await SendErrorAsync(
                webSocket,
                exception.Message
            );
        }
        catch (HttpRequestException)
        {
            await SendErrorAsync(
                webSocket,
                "GameService is not available"
            );
        }
        catch (Exception exception)
        {
            await SendErrorAsync(
                webSocket,
                $"Unexpected error: {exception.Message}"
            );
        }
    }

    private static async Task SendErrorAsync(WebSocket webSocket, string message)
    {
        var payload = new
        {
            type = "Error",
            message
        };

        await SendJsonAsync(webSocket, payload);
    }

    private static async Task SendJsonAsync(
        WebSocket webSocket,
        object payload)
    {
        var json = JsonSerializer.Serialize(
            payload,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }
        );

        var bytes = Encoding.UTF8.GetBytes(json);

        await webSocket.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None
        );
    }
}