namespace DataServer.Application.Configuration;

public class ConnectionManagerSettings
{
    public const string SectionName = "ConnectionManager";

    public int InitialDelaySeconds { get; set; } = 5;
    public int MaxDelaySeconds { get; set; } = 300;
}
