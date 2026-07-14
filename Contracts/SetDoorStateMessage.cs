namespace RealtimeGateway.Contracts;

public class SetDoorStateMessage
{
    public string Type { get; set; } = "SetDoorStateCommand";

    public string MatchId { get; set; } = string.Empty;

    public string DoorId { get; set; } = string.Empty;

    public bool IsClosed { get; set; }
}