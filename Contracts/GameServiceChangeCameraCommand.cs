namespace RealtimeGateway.Contracts;

public class GameServiceChangeCameraCommand
{
    public string PlayerId { get; set; } =
        string.Empty;

    public string? CameraId { get; set; }
}