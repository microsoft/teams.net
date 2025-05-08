using System.Text.Json.Serialization;

namespace Microsoft.Teams.Plugins.AspNetCore.DevTools.Models;

public class MetaData
{
    [JsonPropertyName("id")]
    [JsonPropertyOrder(0)]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    [JsonPropertyOrder(1)]
    public string? Name { get; set; }

    [JsonPropertyName("pages")]
    [JsonPropertyOrder(2)]
    public IList<Page> Pages { get; set; } = [];
}