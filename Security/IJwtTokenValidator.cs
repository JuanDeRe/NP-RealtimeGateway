namespace RealtimeGateway.Security;

public interface IJwtTokenValidator
{
    AuthenticatedConnection? ValidateToken(string token);
}