namespace RealtimeGateway.Models;

public class MatchRoute
{
    public string MatchId { get; set; } =
        string.Empty;

    public string GameServiceId { get; set; } =
        string.Empty;

    public string InternalUrl { get; set; } =
        string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}