FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY RealtimeGateway.csproj ./
RUN dotnet restore RealtimeGateway.csproj

COPY . .
RUN dotnet publish RealtimeGateway.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "RealtimeGateway.dll"]