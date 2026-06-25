// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Core.Http;

/// <summary>
/// Well-known keys and helpers for the per-request property bag carried on
/// <see cref="BotRequestOptions.RequestProperties"/>. Each entry is stamped onto the outbound
/// <see cref="System.Net.Http.HttpRequestMessage.Options"/>, where a
/// <see cref="System.Net.Http.DelegatingHandler"/> can read it (for example, to authenticate as a
/// specific bot app).
/// </summary>
/// <remarks>
/// This is the single per-request extensibility point: known values such as the agentic identity and
/// the bot app id are derived here, and callers can add their own keys. The values flow as a parameter
/// (no ambient state), the same way the activity-derived agentic identity already flows.
/// </remarks>
public static class BotRequestProperties
{
    /// <summary>
    /// Key for the <see cref="AgenticIdentity"/> value. Matches the option key the bot authentication
    /// handler reads, so a stamped bag entry is observed as the agentic identity.
    /// </summary>
    public const string AgenticIdentityKey = "AgenticIdentity";

    /// <summary>
    /// Key for the bot application (client) id, sourced from the incoming Teams activity.
    /// </summary>
    public const string BotAppIdKey = "botAppId";

    /// <summary>
    /// Builds a property bag for an <b>outbound</b> activity: the bot is the sender, so the agentic
    /// identity and bot app id are both derived from <see cref="CoreActivity.From"/>.
    /// </summary>
    /// <param name="activity">The outbound activity, or null.</param>
    /// <returns>The property bag, or null when nothing could be derived.</returns>
    public static IReadOnlyDictionary<string, object?>? FromActivity(CoreActivity? activity)
        => Build(AgenticIdentity.FromAccount(activity?.From), NormalizeAppId(activity?.From?.Id));

    /// <summary>
    /// Builds a property bag for an <b>inbound</b> activity: the bot is the recipient, so the bot app id
    /// comes from <see cref="CoreActivity.Recipient"/> while the agentic identity comes from
    /// <see cref="CoreActivity.From"/>. Use this to capture the per-turn values from the activity that
    /// triggered the turn.
    /// </summary>
    /// <param name="activity">The inbound activity, or null.</param>
    /// <returns>The property bag, or null when nothing could be derived.</returns>
    public static IReadOnlyDictionary<string, object?>? FromInboundActivity(CoreActivity? activity)
        => Build(AgenticIdentity.FromAccount(activity?.From), NormalizeAppId(activity?.Recipient?.Id));

    /// <summary>
    /// Builds a property bag carrying only the supplied agentic identity.
    /// </summary>
    /// <param name="agenticIdentity">The agentic identity, or null.</param>
    /// <returns>The property bag, or null when <paramref name="agenticIdentity"/> is null.</returns>
    public static IReadOnlyDictionary<string, object?>? FromAgenticIdentity(AgenticIdentity? agenticIdentity)
        => Build(agenticIdentity, null);

    /// <summary>
    /// Builds a property bag carrying only the supplied bot app id, used as-is (no channel-prefix
    /// stripping). Use for proactive flows where the bot app id is known directly rather than derived
    /// from an activity.
    /// </summary>
    /// <param name="botAppId">The bot application (client) id, or null.</param>
    /// <returns>The property bag, or null when <paramref name="botAppId"/> is null or empty.</returns>
    public static IReadOnlyDictionary<string, object?>? FromBotAppId(string? botAppId)
    {
        if (string.IsNullOrEmpty(botAppId))
        {
            return null;
        }
        return new Dictionary<string, object?>(StringComparer.Ordinal) { [BotAppIdKey] = botAppId };
    }

    /// <summary>
    /// Merges two property bags, with entries from <paramref name="overrides"/> taking precedence.
    /// </summary>
    /// <param name="baseProperties">The base properties, or null.</param>
    /// <param name="overrides">The overriding properties, or null.</param>
    /// <returns>The merged bag, or null when both inputs are empty.</returns>
    public static IReadOnlyDictionary<string, object?>? Merge(
        IReadOnlyDictionary<string, object?>? baseProperties,
        IReadOnlyDictionary<string, object?>? overrides)
    {
        if (baseProperties is null || baseProperties.Count == 0)
        {
            return overrides is { Count: > 0 } ? overrides : null;
        }

        if (overrides is null || overrides.Count == 0)
        {
            return baseProperties;
        }

        Dictionary<string, object?> merged = new(StringComparer.Ordinal);
        foreach (KeyValuePair<string, object?> entry in baseProperties)
        {
            merged[entry.Key] = entry.Value;
        }
        foreach (KeyValuePair<string, object?> entry in overrides)
        {
            merged[entry.Key] = entry.Value;
        }
        return merged;
    }

    /// <summary>
    /// Gets the agentic identity from a property bag, or null when absent.
    /// </summary>
    public static AgenticIdentity? GetAgenticIdentity(this IReadOnlyDictionary<string, object?> properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        return properties.TryGetValue(AgenticIdentityKey, out object? value) ? value as AgenticIdentity : null;
    }

    /// <summary>
    /// Gets the bot app id from a property bag, or null when absent.
    /// </summary>
    public static string? GetBotAppId(this IReadOnlyDictionary<string, object?> properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        return properties.TryGetValue(BotAppIdKey, out object? value) ? value as string : null;
    }

    private static Dictionary<string, object?>? Build(AgenticIdentity? agenticIdentity, string? botAppId)
    {
        if (agenticIdentity is null && string.IsNullOrEmpty(botAppId))
        {
            return null;
        }

        Dictionary<string, object?> properties = new(StringComparer.Ordinal);
        if (agenticIdentity is not null)
        {
            properties[AgenticIdentityKey] = agenticIdentity;
        }
        if (!string.IsNullOrEmpty(botAppId))
        {
            properties[BotAppIdKey] = botAppId;
        }
        return properties;
    }

    // Teams channel accounts carry the bot id as "28:<appId>"; strip the channel prefix when present.
    private static string? NormalizeAppId(string? id)
        => string.IsNullOrEmpty(id)
            ? null
            : id.StartsWith("28:", StringComparison.Ordinal) ? id[3..] : id;
}
