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
        var match = await GetMatchAsync(matchId);

        if (match is null)
        {
            return false;
        }

        return match.Players.Any(
            player => player.PlayerId == playerId
        );
    }
    public async Task<GameSessionResponse?> GetMatchAsync(
        string matchId)
    {
        try
        {
            var url =
                $"{_options.BaseUrl}/game/matches/{matchId}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content
                .ReadFromJsonAsync<GameSessionResponse>();
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
    }
    public async Task<DoorStateChangedEvent?> SetDoorStateAsync(
        SetDoorStateMessage message,
        string playerId)
    {
        var command = new GameServiceSetDoorStateCommand
        {
            PlayerId = playerId,
            DoorId = message.DoorId,
            IsClosed = message.IsClosed
        };

        var url =
            $"{_options.BaseUrl}" +
            $"/game/matches/{message.MatchId}" +
            "/gameplay/doors/state";

        var response = await _httpClient.PostAsJsonAsync(
            url,
            command
        );

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content
                .ReadFromJsonAsync<ErrorResponse>();

            throw new InvalidOperationException(
                error?.Message ??
                "GameService rejected the door command."
            );
        }

        return await response.Content
            .ReadFromJsonAsync<DoorStateChangedEvent>();
    }
    public async Task<ActiveCameraChangedEvent?>
        ChangeCameraAsync(
            ChangeCameraMessage message,
            string playerId)
    {
        var command = new GameServiceChangeCameraCommand
        {
            PlayerId = playerId,
            CameraId = message.CameraId
        };

        var url =
            $"{_options.BaseUrl}" +
            $"/game/matches/{message.MatchId}" +
            "/gameplay/cameras/active";

        var response = await _httpClient.PostAsJsonAsync(
            url,
            command
        );

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content
                .ReadFromJsonAsync<ErrorResponse>();

            throw new InvalidOperationException(
                error?.Message ??
                "GameService rejected the camera command."
            );
        }

        return await response.Content
            .ReadFromJsonAsync<ActiveCameraChangedEvent>();
    }
}