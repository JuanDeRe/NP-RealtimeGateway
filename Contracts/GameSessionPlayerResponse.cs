namespace RealtimeGateway.Contracts;

public class GameSessionPlayerResponse
{
    public string PlayerId { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string AnimatronicType { get; set; } = string.Empty;

    public GameSessionPositionResponse Position { get; set; } = new();

    public string CurrentRoomId { get; set; } = string.Empty;

    public bool IsConnected { get; set; }
}