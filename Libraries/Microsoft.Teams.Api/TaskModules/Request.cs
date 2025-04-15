using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.TaskModules;

/// <summary>
/// Task module invoke request value payload
/// </summary>
public class Request
{
    /// <summary>
    /// User input data. Free payload with key-value pairs.
    /// </summary>
    [JsonPropertyName("data")]
    [JsonPropertyOrder(0)]
    public object? Data { get; set; }

    /// <summary>
    /// Current user context, i.e., the current theme
    /// </summary>
    [JsonPropertyName("context")]
    [JsonPropertyOrder(1)]
    public RequestContext? Context { get; set; }
}

/// <summary>
/// Current user context, i.e., the current theme
/// </summary>
public class RequestContext
{
    /// <summary>
    /// the users current theme
    /// </summary>
    [JsonPropertyName("theme")]
    [JsonPropertyOrder(0)]
    public string? Theme { get; set; }
}