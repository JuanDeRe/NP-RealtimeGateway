using RealtimeGateway.Configuration;
using RealtimeGateway.Services;
using RealtimeGateway.WebSockets;
using RealtimeGateway.Configuration;
using RealtimeGateway.Security;
using RealtimeGateway.Services;
using RealtimeGateway.WebSockets;

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

var app = builder.Build();

app.UseWebSockets();

app.MapGet("/", () => "Nightwatch Protocol - Realtime Gateway running");

app.Map("/game-ws", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("WebSocket request expected");
        return;
    }

    var token = context.Request.Query["access_token"].ToString();

    if (string.IsNullOrWhiteSpace(token))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync("Missing access_token");
        return;
    }

    var tokenValidator = context.RequestServices
        .GetRequiredService<IJwtTokenValidator>();

    var authenticatedConnection = tokenValidator.ValidateToken(token);

    if (authenticatedConnection is null)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync("Invalid token");
        return;
    }

    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();

    var handler = context.RequestServices
        .GetRequiredService<GameWebSocketHandler>();

    await handler.HandleAsync(webSocket, authenticatedConnection);
});

app.Run();