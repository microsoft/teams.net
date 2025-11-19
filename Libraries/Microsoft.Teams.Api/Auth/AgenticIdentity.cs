namespace Microsoft.Teams.Api.Auth;

public class AgenticIdentity
{
    public string? AgentticAppId { get; set; }
    public string? AgenticUserId { get; set; }
    public string? AgenticAppBlueprintId { get; set; }
    public string? TenantId { get; set; }

    public static AgenticIdentity FromProperties(IDictionary<string, object> properties)
    {
        properties.TryGetValue("agenticAppId", out object? appIdObj);
        properties.TryGetValue("agenticUserId", out object? userIdObj);
        properties.TryGetValue("agenticAppBlueprintId", out object? bluePrintObj);
        return new AgenticIdentity
        {
            AgentticAppId = appIdObj?.ToString(),
            AgenticUserId = userIdObj?.ToString(),
            AgenticAppBlueprintId = bluePrintObj?.ToString()
        };
    }
}
