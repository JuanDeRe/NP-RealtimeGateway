namespace RealtimeGateway.Contracts;

public class GameServiceMoveCommand
{
    public string PlayerId { get; set; } = string.Empty;

    public float DirectionX { get; set; }

    public float DirectionY { get; set; }

    public float DeltaTime { get; set; } = 0.016f;
}