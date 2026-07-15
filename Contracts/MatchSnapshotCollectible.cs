namespace RealtimeGateway.Contracts;

public class MatchSnapshotCollectible
{
    public string CollectibleId { get; set; } =
        string.Empty;

    public string Type { get; set; } =
        string.Empty;

    public string OwnerPlayerId { get; set; } =
        string.Empty;

    public float X { get; set; }

    public float Y { get; set; }

    public string RoomId { get; set; } =
        string.Empty;
}