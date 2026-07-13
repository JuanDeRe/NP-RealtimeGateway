namespace RealtimeGateway.Contracts;

public class MatchSnapshotPlayer
{
    public string PlayerId { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string AnimatronicType { get; set; } = string.Empty;

    public float X { get; set; }

    public float Y { get; set; }

    public string RoomId { get; set; } = string.Empty;

    public bool IsConnected { get; set; }
}