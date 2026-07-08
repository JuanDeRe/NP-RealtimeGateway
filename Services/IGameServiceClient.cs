using RealtimeGateway.Contracts;

namespace RealtimeGateway.Services;

public interface IGameServiceClient
{
    Task<PlayerMovedEvent?> MoveAsync(MoveCommandMessage message);
}