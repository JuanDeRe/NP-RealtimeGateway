using RealtimeGateway.Configuration;
using RealtimeGateway.Services;
using RealtimeGateway.WebSockets;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<GameServiceOptions>(
    builder.Configuration.GetSection("GameService")
);

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

    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();

    var handler = context.RequestServices.GetRequiredService<GameWebSocketHandler>();

    await handler.HandleAsync(webSocket);
});

app.Run();