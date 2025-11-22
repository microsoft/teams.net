namespace Microsoft.Teams.Api.Auth;

public class AgenticIdentity
{
    public string? AgenticAppId { get; set; }
    public string? AgenticUserId { get; set; }
    public string? AgenticAppBlueprintId { get; set; }
    public string? TenantId { get; set; }

    public static AgenticIdentity? FromProperties(IDictionary<string, object> properties)
    {
        if (properties == null)
        {
            return null;
        }

        properties.TryGetValue("agenticAppId", out object? appIdObj);
        properties.TryGetValue("agenticUserId", out object? userIdObj);
        properties.TryGetValue("agenticAppBlueprintId", out object? bluePrintObj);
        return new AgenticIdentity
        {
            AgenticAppId = appIdObj?.ToString(),
            AgenticUserId = userIdObj?.ToString(),
            AgenticAppBlueprintId = bluePrintObj?.ToString()
        };
    }
}
