namespace RealtimeGateway.Contracts;

public class MatchFinishedEvent
{
    public string Type { get; set; } =
        "MatchFinished";

    public string EventId { get; set; } =
        string.Empty;

    public string MatchId { get; set; } =
        string.Empty;

    public string Winner { get; set; } =
        string.Empty;

    public List<string> PlayerIds { get; set; } = [];

    public DateTime FinishedAtUtc { get; set; }
}