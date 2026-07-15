namespace RealtimeGateway.Contracts;

public class PlayerMovedEvent
{
    public string Type { get; set; } = "PlayerMoved";

    public string MatchId { get; set; } = string.Empty;

    public string PlayerId { get; set; } = string.Empty;

    public float X { get; set; }

    public float Y { get; set; }

    public string RoomId { get; set; } = string.Empty;
    public string? CollectedCollectibleId { get; set; }

    public int CollectedItems { get; set; }

    public int RequiredItems { get; set; }

    public bool IsEmpowered { get; set; }
}