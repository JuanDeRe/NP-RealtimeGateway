namespace RealtimeGateway.Contracts;

public class DoorStateChangedEvent
{
    public string Type { get; set; } = "DoorStateChanged";

    public string MatchId { get; set; } = string.Empty;

    public string DoorId { get; set; } = string.Empty;

    public bool IsClosed { get; set; }

    public string ChangedByPlayerId { get; set; } = string.Empty;
}