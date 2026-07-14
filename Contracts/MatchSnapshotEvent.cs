namespace RealtimeGateway.Contracts;

public class MatchSnapshotEvent
{
    public string Type { get; set; } = "MatchSnapshot";

    public string MatchId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int DurationSeconds { get; set; }

    public DateTime? StartedAtUtc { get; set; }

    public List<MatchSnapshotPlayer> Players { get; set; } = [];
    
    public List<MatchSnapshotDoor> Doors { get; set; } = [];
}