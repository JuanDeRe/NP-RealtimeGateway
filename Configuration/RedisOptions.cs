namespace RealtimeGateway.Configuration;

public class RedisOptions
{
    public string ConnectionString { get; set; } =
        "localhost:6379";

    public string InstanceName { get; set; } =
        "nightwatch:";

    public string ConsumerId { get; set; } =
        "gateway-local";
}