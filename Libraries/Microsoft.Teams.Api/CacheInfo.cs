using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api;

/// <summary>
/// A cache info object which notifies Teams how long an object should be cached for.
/// </summary>
public class CacheInfo
{
    /// <summary>
    /// The type of cache for this object.
    /// </summary>
    [JsonPropertyName("cacheType")]
    [JsonPropertyOrder(0)]
    public string? CacheType { get; set; }

    /// <summary>
    /// The time in seconds for which the cached object should remain in the cache
    /// </summary>
    [JsonPropertyName("cacheDuration")]
    [JsonPropertyOrder(1)]
    public int? CacheDuration { get; set; }
}