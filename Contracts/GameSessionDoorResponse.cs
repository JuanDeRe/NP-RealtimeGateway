namespace RealtimeGateway.Contracts;

public class GameSessionDoorResponse
{
    public string DoorId { get; set; } = string.Empty;

    public bool IsClosed { get; set; }
}