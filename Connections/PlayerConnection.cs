using System.Net.WebSockets;

namespace RealtimeGateway.Connections;

public class PlayerConnection
{
    public string ConnectionId { get; set; } = string.Empty;

    public string PlayerId { get; set; } = string.Empty;

    public string MatchId { get; set; } = string.Empty;

    public WebSocket WebSocket { get; set; } = null!;

    // Evita envíos simultáneos sobre el mismo WebSocket.
    public SemaphoreSlim SendLock { get; } = new(1, 1);
}