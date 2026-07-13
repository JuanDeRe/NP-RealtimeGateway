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

    public async Task<PlayerMovedEvent?> MoveAsync(
        MoveCommandMessage message,
        string playerId)
    {
        var request = new GameServiceMoveCommand
        {
            PlayerId = playerId,
            DirectionX = message.DirectionX,
            DirectionY = message.DirectionY,
            DeltaTime = message.DeltaTime
        };

        var url = $"{_options.BaseUrl}/game/matches/{message.MatchId}/gameplay/move";

        var response = await _httpClient.PostAsJsonAsync(url, request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

            throw new InvalidOperationException(
                error?.Message ?? "GameService rejected movement"
            );
        }

        return await response.Content.ReadFromJsonAsync<PlayerMovedEvent>();
    }
    public async Task<bool> IsPlayerInMatchAsync(
        string matchId,
        string playerId)
    {
        try
        {
            var url =
                $"{_options.BaseUrl}/game/matches/{matchId}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var match = await response.Content
                .ReadFromJsonAsync<GameSessionResponse>();

            if (match is null)
            {
                return false;
            }

            return match.Players.Any(
                player => player.PlayerId == playerId
            );
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }
}