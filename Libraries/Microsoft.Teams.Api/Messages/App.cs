using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Messages;

/// <summary>
/// The type of
/// application. Possible values include: 'aadApplication', 'bot',
/// 'tenantBot', 'office365Connector', 'webhook'
/// </summary>
[JsonConverter(typeof(JsonConverter<AppIdentityType>))]
public class AppIdentityType(string value) : StringEnum(value)
{
    public static readonly AppIdentityType AadApplication = new("aadApplication");
    public bool IsAadApplication => AadApplication.Equals(Value);

    public static readonly AppIdentityType Bot = new("bot");
    public bool IsBot => Bot.Equals(Value);

    public static readonly AppIdentityType TenantBot = new("tenantBot");
    public bool IsTenantBot => TenantBot.Equals(Value);

    public static readonly AppIdentityType O365Connector = new("office365Connector");
    public bool IsO365Connector => O365Connector.Equals(Value);

    public static readonly AppIdentityType Webhook = new("webhook");
    public bool IsWebhook => Webhook.Equals(Value);
}

/// <summary>
/// Represents an application entity.
/// </summary>
public class App
{
    /// <summary>
    /// The type of
    /// application. Possible values include: 'aadApplication', 'bot',
    /// 'tenantBot', 'office365Connector', 'webhook'
    /// </summary>
    [JsonPropertyName("applicationIdentityType")]
    [JsonPropertyOrder(0)]
    public AppIdentityType? ApplicationIdentityType { get; set; }

    /// <summary>
    /// The id of the application.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonPropertyOrder(1)]
    public required string Id { get; set; }

    /// <summary>
    /// The plaintext display name of the application.
    /// </summary>
    [JsonPropertyName("displayName")]
    [JsonPropertyOrder(2)]
    public string? DisplayName { get; set; }
}