using RealtimeGateway.BackgroundServices;
using RealtimeGateway.Configuration;
using RealtimeGateway.Services;
using RealtimeGateway.WebSockets;
using RealtimeGateway.Configuration;
using RealtimeGateway.Security;
using RealtimeGateway.Services;
using RealtimeGateway.WebSockets;
using RealtimeGateway.Connections;
using RealtimeGateway.Contracts;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<GameServiceOptions>(
    builder.Configuration.GetSection("GameService")
);

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection("Jwt")
);

builder.Services.AddSingleton<IJwtTokenValidator, JwtTokenValidator>();
builder.Services.AddHttpClient<IGameServiceClient, GameServiceClient>();

builder.Services.AddScoped<GameWebSocketHandler>();

builder.Services.Configure<RedisOptions>(
    builder.Configuration.GetSection("Redis")
);

builder.Services.AddSingleton<IConnectionMultiplexer>(
    serviceProvider =>
    {
        var options = serviceProvider
            .GetRequiredService<
                Microsoft.Extensions.Options.IOptions<
                    RedisOptions
                >
            >()
            .Value;

        var configuration =
            ConfigurationOptions.Parse(
                options.ConnectionString
            );

        configuration.AbortOnConnectFail = false;
        configuration.ConnectRetry = 3;
        configuration.ConnectTimeout = 5000;

        return ConnectionMultiplexer.Connect(
            configuration
        );
    }
);
builder.Services.AddHostedService<
    MatchEventStreamProcessor
>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("UnityWebClient", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddSingleton<
    IConnectionManager,
    ConnectionManager
>();

var app = builder.Build();

app.UseCors("UnityWebClient");

app.UseWebSockets();

app.MapGet("/", () => "Nightwatch Protocol - Realtime Gateway running");

app.Map("/game-ws", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode =
            StatusCodes.Status400BadRequest;

        await context.Response.WriteAsync(
            "WebSocket request expected"
        );

        return;
    }

    var token = context.Request.Query[
        "access_token"
    ].ToString();

    var matchId = context.Request.Query[
        "match_id"
    ].ToString();

    if (string.IsNullOrWhiteSpace(token))
    {
        context.Response.StatusCode =
            StatusCodes.Status401Unauthorized;

        await context.Response.WriteAsync(
            "Missing access_token"
        );

        return;
    }

    if (string.IsNullOrWhiteSpace(matchId))
    {
        context.Response.StatusCode =
            StatusCodes.Status400BadRequest;

        await context.Response.WriteAsync(
            "Missing match_id"
        );

        return;
    }

    var tokenValidator = context.RequestServices
        .GetRequiredService<IJwtTokenValidator>();

    var authenticatedConnection =
        tokenValidator.ValidateToken(token);

    if (authenticatedConnection is null)
    {
        context.Response.StatusCode =
            StatusCodes.Status401Unauthorized;

        await context.Response.WriteAsync(
            "Invalid token"
        );

        return;
    }

    var gameServiceClient = context.RequestServices
        .GetRequiredService<IGameServiceClient>();

    var belongsToMatch =
        await gameServiceClient.IsPlayerInMatchAsync(
            matchId,
            authenticatedConnection.PlayerId
        );

    if (!belongsToMatch)
    {
        context.Response.StatusCode =
            StatusCodes.Status403Forbidden;

        await context.Response.WriteAsync(
            "Player does not belong to this match"
        );

        return;
    }

    using var webSocket =
        await context.WebSockets.AcceptWebSocketAsync();

    var connectionManager = context.RequestServices
        .GetRequiredService<IConnectionManager>();

    var playerConnection = connectionManager.Add(
        authenticatedConnection.PlayerId,
        matchId,
        webSocket
    );

    var currentMatch =
        await gameServiceClient.GetMatchAsync(matchId);
    

    if (currentMatch is not null)
    {
        var serverNowUtc = DateTime.UtcNow;
        var endsAtUtc = currentMatch.StartedAtUtc?.AddSeconds(currentMatch.DurationSeconds);
        var snapshot = new MatchSnapshotEvent
        {
            MatchId = currentMatch.MatchId,
            Status = currentMatch.Status,
            DurationSeconds = currentMatch.DurationSeconds,
            StartedAtUtc = currentMatch.StartedAtUtc,
            ServerNowUtc = serverNowUtc,
            EndsAtUtc = endsAtUtc,
            ActiveCameraId = currentMatch.ActiveCameraId,
            Doors = currentMatch.Doors
                .Select(door => new MatchSnapshotDoor
                {
                    DoorId = door.DoorId,
                    IsClosed = door.IsClosed
                })
                .ToList(),

            Players = currentMatch.Players
                .Select(player => new MatchSnapshotPlayer
                {
                    PlayerId = player.PlayerId,
                    Role = player.Role,
                    AnimatronicType = player.AnimatronicType,
                    X = player.Position.X,
                    Y = player.Position.Y,
                    RoomId = player.CurrentRoomId,
                    IsConnected = player.IsConnected
                })
                .ToList(),
            Collectibles = currentMatch.Collectibles
            .Where(collectible =>
            collectible.OwnerPlayerId ==
            authenticatedConnection.PlayerId &&
            !collectible.IsCollected
            )
            .Select(collectible =>
            new MatchSnapshotCollectible
            {
            CollectibleId =
            collectible.CollectibleId,

            Type = collectible.Type,

            OwnerPlayerId =
                collectible.OwnerPlayerId,

            X = collectible.Position.X,
            Y = collectible.Position.Y,

            RoomId = collectible.RoomId
        })
        .ToList()
        };
        await connectionManager.SendToConnectionAsync(
            playerConnection,
            snapshot
        );
    }

    var handler = context.RequestServices
        .GetRequiredService<GameWebSocketHandler>();

    try
    {
        await handler.HandleAsync(
            webSocket,
            authenticatedConnection,
            matchId
        );
    }
    finally
    {
        connectionManager.Remove(
            playerConnection.ConnectionId
        );
    }
});

app.Run();