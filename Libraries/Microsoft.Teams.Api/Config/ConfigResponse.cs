using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Config;

/// <summary>
/// Envelope for Config Response Payload.
/// </summary>
public class ConfigResponse
{
    /// <summary>
    /// Gets or sets response type invoke request.
    /// </summary>
    /// <value> Invoke request response type.</value>
    [JsonPropertyName("responseType")]
    [JsonPropertyOrder(0)]
    public string ResponseType { get; set; } = "config";

    /// <summary>
    /// Gets or sets the response to the config message.
    /// Possible values for the config type include: 'auth'or 'task'.
    /// </summary>
    /// <value>
    /// Response to a config request.
    /// </value>
    [JsonPropertyName("config")]
    [JsonPropertyOrder(1)]
    public object? Config { get; set; }

    /// <summary>
    /// Gets or sets response cache Info.
    /// </summary>
    /// <value> Value of cache info. </value>
    [JsonPropertyName("cacheInfo")]
    [JsonPropertyOrder(2)]
    public CacheInfo? CacheInfo { get; set; }
}

/// <summary>
/// Envelope for Config Response Payload.
/// </summary>
public class ConfigResponse<T>(T config) : ConfigResponse where T : notnull
{
    /// <summary>
    /// Gets or sets the response to the config message.
    /// Possible values for the config type include: 'auth'or 'task'.
    /// </summary>
    /// <value>
    /// Response to a config request.
    /// </value>
    [JsonPropertyName("config")]
    [JsonPropertyOrder(1)]
    public new T Config { get; set; } = config;
}