using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using RealtimeGateway.Configuration;
using RealtimeGateway.Contracts;

namespace RealtimeGateway.Services;

public class GameServiceClient : IGameServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly GameServiceOptions _options;

    public GameServiceClient(
        HttpClient httpClient,
        IOptions<GameServiceOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<PlayerMovedEvent?> MoveAsync(MoveCommandMessage message)
    {
        var request = new GameServiceMoveCommand
        {
            PlayerId = message.PlayerId,
            DirectionX = message.DirectionX,
            DirectionY = message.DirectionY,
            DeltaTime = message.DeltaTime
        };

        var url = $"{_options.BaseUrl}/game/matches/{message.MatchId}/gameplay/move";

        var response = await _httpClient.PostAsJsonAsync(url, request);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<PlayerMovedEvent>();
    }
}