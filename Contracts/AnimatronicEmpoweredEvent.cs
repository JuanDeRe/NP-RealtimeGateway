namespace RealtimeGateway.Contracts;

public class AnimatronicEmpoweredEvent
{
    public string Type { get; set; } =
        "AnimatronicEmpowered";

    public string MatchId { get; set; } =
        string.Empty;

    public string PlayerId { get; set; } =
        string.Empty;

    public string AnimatronicType { get; set; } =
        string.Empty;
}