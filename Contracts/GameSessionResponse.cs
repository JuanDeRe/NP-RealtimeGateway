namespace RealtimeGateway.Contracts;

public class GameSessionResponse
{
    public string MatchId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Winner { get; set; } = string.Empty;

    public int DurationSeconds { get; set; }

    public DateTime? StartedAtUtc { get; set; }

    public DateTime? FinishedAtUtc { get; set; }

    public List<GameSessionPlayerResponse> Players { get; set; } = [];
    
    public List<GameSessionDoorResponse> Doors { get; set; } = [];
    
    public string? ActiveCameraId { get; set; }
}