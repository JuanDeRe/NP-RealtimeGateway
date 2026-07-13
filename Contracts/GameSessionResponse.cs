namespace RealtimeGateway.Contracts;

public class GameSessionResponse
{
    public string MatchId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public List<GameSessionPlayerResponse> Players { get; set; } = [];
}