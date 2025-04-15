using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api;

public class Account
{
    [JsonPropertyName("id")]
    [JsonPropertyOrder(0)]
    public required string Id { get; set; }

    [JsonPropertyName("aadObjectId")]
    [JsonPropertyOrder(1)]
    public string? AadObjectId { get; set; }

    [JsonPropertyName("role")]
    [JsonPropertyOrder(2)]
    public Role? Role { get; set; }

    [JsonPropertyName("name")]
    [JsonPropertyOrder(3)]
    public string? Name { get; set; }

    [JsonPropertyName("properties")]
    [JsonPropertyOrder(4)]
    public Dictionary<string, dynamic>? Properties { get; set; }
}

[JsonConverter(typeof(JsonConverter<Role>))]
public class Role(string value) : StringEnum(value)
{
    public static readonly Role Bot = new("bot");
    public bool IsBot => Bot.Equals(Value);

    public static readonly Role User = new("user");
    public bool IsUser => User.Equals(Value);
}