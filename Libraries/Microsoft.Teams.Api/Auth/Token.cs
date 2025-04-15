using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Auth;

public interface IToken
{
    public string? AppId { get; }
    public string? AppDisplayName { get; }
    public string? TenantId { get; }
    public string ServiceUrl { get; }
    public CallerType From { get; }
    public string FromId { get; }
    public string ToString();
}

[JsonConverter(typeof(JsonConverter<CallerType>))]
public class CallerType(string value) : Common.StringEnum(value)
{
    public static readonly CallerType Bot = new("bot");
    public bool IsBot => Bot.Equals(Value);

    public static readonly CallerType Azure = new("azure");
    public bool IsAzure => Azure.Equals(Value);

    public static readonly CallerType Gov = new("gov");
    public bool IsGov => Gov.Equals(Value);
}