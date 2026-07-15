namespace RealtimeGateway.Contracts;

public class GameSessionCollectibleResponse
{
    public string CollectibleId { get; set; } =
        string.Empty;

    public string Type { get; set; } =
        string.Empty;

    public string OwnerPlayerId { get; set; } =
        string.Empty;

    public GameSessionPositionResponse Position {
        get;
        set;
    } = new();

    public string RoomId { get; set; } =
        string.Empty;

    public bool IsCollected { get; set; }

    public DateTime? CollectedAtUtc { get; set; }
}