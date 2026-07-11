namespace RealtimeGateway.Contracts;

public class MoveCommandMessage
{
    public string Type { get; set; } = "MoveCommand";

    public string MatchId { get; set; } = string.Empty;

    public float DirectionX { get; set; }

    public float DirectionY { get; set; }

    public float DeltaTime { get; set; } = 0.016f;
}