namespace RealtimeGateway.Security;

public class AuthenticatedConnection
{
    public string PlayerId { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}