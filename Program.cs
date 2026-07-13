using RealtimeGateway.Configuration;
using RealtimeGateway.Services;
using RealtimeGateway.WebSockets;
using RealtimeGateway.Configuration;
using RealtimeGateway.Security;
using RealtimeGateway.Services;
using RealtimeGateway.WebSockets;
using RealtimeGateway.Connections;

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