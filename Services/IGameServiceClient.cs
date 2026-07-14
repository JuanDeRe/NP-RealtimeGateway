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
    Task<GameSessionResponse?> GetMatchAsync(string matchId);
    
    Task<DoorStateChangedEvent?> SetDoorStateAsync(
        SetDoorStateMessage message,
        string playerId
    );
    
    Task<ActiveCameraChangedEvent?> ChangeCameraAsync(
            ChangeCameraMessage message,
            string playerId
    );
        
}