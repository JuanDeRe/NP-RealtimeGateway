using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using RealtimeGateway.Contracts;
using RealtimeGateway.Services;

namespace RealtimeGateway.WebSockets;

public class GameWebSocketHandler
{
    private readonly IGameServiceClient _gameServiceClient;

    public GameWebSocketHandler(IGameServiceClient gameServiceClient)
    {
        _gameServiceClient = gameServiceClient;
    }

    public async Task HandleAsync(WebSocket webSocket)
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
                await HandleMoveCommandAsync(webSocket, json);
                continue;
            }

            await SendErrorAsync(webSocket, $"Unsupported message type: {baseMessage.Type}");
        }
    }

    private async Task HandleMoveCommandAsync(WebSocket webSocket, string json)
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
            await SendErrorAsync(webSocket, "Invalid MoveCommand payload");
            return;
        }
        
        
        try
        
        {
            var movementEvent = await _gameServiceClient.MoveAsync(command);

            if (movementEvent is null)
            {
                await SendErrorAsync(webSocket, "GameService rejected movement");
                return;
            }

            await SendJsonAsync(webSocket, movementEvent);
        }
        catch (HttpRequestException)
        {
            await SendErrorAsync(webSocket, "GameService is not available");
        }
        catch (Exception exception)
        {
            await SendErrorAsync(webSocket, $"Unexpected error: {exception.Message}");
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

    private static async Task SendJsonAsync(WebSocket webSocket, object payload)
    {
        var json = JsonSerializer.Serialize(payload);

        var bytes = Encoding.UTF8.GetBytes(json);

        await webSocket.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None
        );
    }
}