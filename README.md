# Nightwatch Protocol - RealtimeGateway

https://github.com/JuanDeRe/NP-RealtimeGateway/actions/workflows/ci.yml/badge.svg](https://github.com/JuanDeRe/NP-RealtimeGateway/actions/workflows/ci.yml)

`RealtimeGateway` es el servicio encargado de manejar conexiones WebSocket entre los clientes y el backend de gameplay.

El Gateway no contiene reglas de juego. Su función es recibir mensajes en tiempo real desde el cliente, reenviar comandos al GameService y devolver al cliente los eventos autoritativos generados por el servidor.

## Responsabilidades principales

- Aceptar conexiones WebSocket.
- Recibir comandos de gameplay desde clientes.
- Enviar comandos al GameService.
- Recibir eventos autoritativos desde GameService.
- Enviar respuestas al cliente conectado.
- Manejar errores cuando GameService no está disponible.

## Tecnologías

- C# / .NET
- ASP.NET Core
- WebSockets
- HTTP interno hacia GameService

## Estructura

```text
RealtimeGateway/
├── Configuration/
├── Contracts/
├── Services/
├── WebSockets/
├── Program.cs
├── appsettings.json
├── RealtimeGateway.csproj
└── README.md
```