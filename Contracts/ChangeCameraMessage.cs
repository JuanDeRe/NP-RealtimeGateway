namespace RealtimeGateway.Contracts;

public class ChangeCameraMessage
{
    public string Type { get; set; } =
        "ChangeCameraCommand";

    public string MatchId { get; set; } =
        string.Empty;

    public string? CameraId { get; set; }
}