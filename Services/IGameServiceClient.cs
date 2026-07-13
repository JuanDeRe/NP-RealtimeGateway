using RealtimeGateway.Contracts;

namespace RealtimeGateway.Services;

public interface IGameServiceClient
{
    Task<bool> IsPlayerInMatchAsync(
        string matchId,
        string playerId
    );
    Task<PlayerMovedEvent?> MoveAsync(
        MoveCommandMessage message,
        string playerId
    );
}