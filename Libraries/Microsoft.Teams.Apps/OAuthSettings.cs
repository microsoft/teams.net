public class OAuthSettings(string? connectionName = "graph")
{
    public string DefaultConnectionName { get; set; } = connectionName;
}