using RealtimeGateway.Contracts;
using RealtimeGateway.Routing;
using RealtimeGateway.Observability;
namespace RealtimeGateway.Services;

/// <summary>
/// Sends gameplay requests to the GameService instance
/// that owns each match.
/// </summary>
public class GameServiceClient : IGameServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IMatchRouteResolver _matchRouteResolver;
    private readonly ILogger<GameServiceClient> _logger;
    private readonly IGatewayMetrics _metrics;

    public GameServiceClient(
        HttpClient httpClient,
        IMatchRouteResolver matchRouteResolver,
        ILogger<GameServiceClient> logger,
        IGatewayMetrics metrics)
    {
        _httpClient = httpClient;
        _matchRouteResolver = matchRouteResolver;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task<PlayerMovedEvent?> MoveAsync(
        MoveCommandMessage message,
        string playerId)
    {
        var baseUrl = GetRequiredGameServiceUrl(
            message.MatchId
        );

        var command = new GameServiceMoveCommand
        {
            PlayerId = playerId,
            DirectionX = message.DirectionX,
            DirectionY = message.DirectionY,
            DeltaTime = message.DeltaTime
        };

        var url =
            $"{baseUrl}" +
            $"/game/matches/{message.MatchId}" +
            "/gameplay/move";

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                url,
                command
            );

            if (!response.IsSuccessStatusCode)
            {
                _metrics.RecordGameServiceRequestFailure();
                throw await CreateGameServiceExceptionAsync(
                    response,
                    "GameService rejected movement."
                );
            }

            return await response.Content
                .ReadFromJsonAsync<PlayerMovedEvent>();
        }
        catch (HttpRequestException exception)
        {
            _metrics.RecordGameServiceRequestFailure();
            _logger.LogError(
                exception,
                "Could not send movement for match {MatchId} " +
                "to {GameServiceUrl}",
                message.MatchId,
                baseUrl
            );

            throw new InvalidOperationException(
                "GameService is not available.",
                exception
            );
        }
        catch (TaskCanceledException exception)
        {
            _metrics.RecordGameServiceRequestFailure();
            _logger.LogError(
                exception,
                "Movement request timed out for match {MatchId}",
                message.MatchId
            );

            throw new InvalidOperationException(
                "GameService request timed out.",
                exception
            );
        }
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
        string baseUrl;

        try
        {
            baseUrl = GetRequiredGameServiceUrl(matchId);
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogWarning(
                exception,
                "No GameService route is available for match {MatchId}",
                matchId
            );

            return null;
        }

        var url =
            $"{baseUrl}/game/matches/{matchId}";

        try
        {
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _metrics.RecordGameServiceRequestFailure();
                var responseBody = await response.Content
                    .ReadAsStringAsync();

                _logger.LogWarning(
                    "GameService rejected match query. " +
                    "MatchId={MatchId}, Status={StatusCode}, " +
                    "Body={ResponseBody}",
                    matchId,
                    response.StatusCode,
                    responseBody
                );

                return null;
            }

            return await response.Content
                .ReadFromJsonAsync<GameSessionResponse>();
        }
        catch (HttpRequestException exception)
        {
            _metrics.RecordGameServiceRequestFailure();
            _logger.LogError(
                exception,
                "Could not query match {MatchId} from {GameServiceUrl}",
                matchId,
                baseUrl
            );

            return null;
        }
        catch (TaskCanceledException exception)
        {
            _metrics.RecordGameServiceRequestFailure();
            _logger.LogError(
                exception,
                "Match query timed out for match {MatchId}",
                matchId
            );

            return null;
        }
    }

    public async Task<DoorStateChangedEvent?> SetDoorStateAsync(
        SetDoorStateMessage message,
        string playerId)
    {
        var baseUrl = GetRequiredGameServiceUrl(
            message.MatchId
        );

        var command = new GameServiceSetDoorStateCommand
        {
            PlayerId = playerId,
            DoorId = message.DoorId,
            IsClosed = message.IsClosed
        };

        var url =
            $"{baseUrl}" +
            $"/game/matches/{message.MatchId}" +
            "/gameplay/doors/state";

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                url,
                command
            );

            if (!response.IsSuccessStatusCode)
            {
                _metrics.RecordGameServiceRequestFailure();
                throw await CreateGameServiceExceptionAsync(
                    response,
                    "GameService rejected the door command."
                );
            }

            return await response.Content
                .ReadFromJsonAsync<DoorStateChangedEvent>();
        }
        catch (HttpRequestException exception)
        {
            _metrics.RecordGameServiceRequestFailure();
            _logger.LogError(
                exception,
                "Could not update door {DoorId} in match {MatchId}",
                message.DoorId,
                message.MatchId
            );

            throw new InvalidOperationException(
                "GameService is not available.",
                exception
            );
        }
        catch (TaskCanceledException exception)
        {
            _metrics.RecordGameServiceRequestFailure();
            _logger.LogError(
                exception,
                "Door request timed out for match {MatchId}",
                message.MatchId
            );

            throw new InvalidOperationException(
                "GameService request timed out.",
                exception
            );
        }
    }

    public async Task<ActiveCameraChangedEvent?>
        ChangeCameraAsync(
            ChangeCameraMessage message,
            string playerId)
    {
        var baseUrl = GetRequiredGameServiceUrl(
            message.MatchId
        );

        var command = new GameServiceChangeCameraCommand
        {
            PlayerId = playerId,
            CameraId = message.CameraId
        };

        var url =
            $"{baseUrl}" +
            $"/game/matches/{message.MatchId}" +
            "/gameplay/cameras/active";

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                url,
                command
            );

            if (!response.IsSuccessStatusCode)
            {
                _metrics.RecordGameServiceRequestFailure();
                throw await CreateGameServiceExceptionAsync(
                    response,
                    "GameService rejected the camera command."
                );
            }

            return await response.Content
                .ReadFromJsonAsync<ActiveCameraChangedEvent>();
        }
        catch (HttpRequestException exception)
        {
            _metrics.RecordGameServiceRequestFailure();
            _logger.LogError(
                exception,
                "Could not change the active camera in match {MatchId}",
                message.MatchId
            );

            throw new InvalidOperationException(
                "GameService is not available.",
                exception
            );
        }
        catch (TaskCanceledException exception)
        {
            _metrics.RecordGameServiceRequestFailure();
            _logger.LogError(
                exception,
                "Camera request timed out for match {MatchId}",
                message.MatchId
            );

            throw new InvalidOperationException(
                "GameService request timed out.",
                exception
            );
        }
    }

    private string GetRequiredGameServiceUrl(
        string matchId)
    {
        if (string.IsNullOrWhiteSpace(matchId))
        {
            _metrics.RecordRoutingFailure();

            throw new InvalidOperationException(
                "MatchId is required to resolve GameService."
            );
        }

        var baseUrl =
            _matchRouteResolver.GetGameServiceUrl(
                matchId
            );

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            _metrics.RecordRoutingFailure();

            throw new InvalidOperationException(
                $"No GameService route was found " +
                $"for match '{matchId}'."
            );
        }

        return baseUrl.TrimEnd('/');
    }

    private static async Task<InvalidOperationException>
        CreateGameServiceExceptionAsync(
            HttpResponseMessage response,
            string fallbackMessage)
    {
        try
        {
            var error = await response.Content
                .ReadFromJsonAsync<ErrorResponse>();

            if (error is not null &&
                !string.IsNullOrWhiteSpace(error.Message))
            {
                return new InvalidOperationException(
                    error.Message
                );
            }
        }
        catch
        {
            // Si la respuesta no era JSON, se intenta leer como texto.
        }

        var responseBody = await response.Content
            .ReadAsStringAsync();

        if (!string.IsNullOrWhiteSpace(responseBody))
        {
            return new InvalidOperationException(
                responseBody
            );
        }

        return new InvalidOperationException(
            fallbackMessage
        );
    }
}