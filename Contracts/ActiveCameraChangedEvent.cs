namespace RealtimeGateway.Contracts;

public class ActiveCameraChangedEvent
{
    public string Type { get; set; } =
        "ActiveCameraChanged";

    public string MatchId { get; set; } =
        string.Empty;

    public string? CameraId { get; set; }

    public string ChangedByPlayerId { get; set; } =
        string.Empty;
}