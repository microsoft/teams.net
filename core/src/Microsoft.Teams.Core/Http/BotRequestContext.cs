// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Core.Http;

/// <summary>
/// Typed per-request context carried on <see cref="BotRequestOptions.RequestContext"/>. Each field is stamped onto
/// the outbound <see cref="System.Net.Http.HttpRequestMessage.Options"/>, where a
/// <see cref="System.Net.Http.DelegatingHandler"/> can read it (for example, to authenticate as a specific bot app).
/// </summary>
/// <remarks>
/// The well-known values (agentic identity, bot app id) are derived from the activity via the factory methods, or set
/// directly. The values flow as a parameter (no ambient state).
/// </remarks>
public record BotRequestContext
{
    /// <summary>
    /// Option key under which <see cref="AgenticIdentity"/> is stamped onto the request options. Matches the key the
    /// bot authentication handler reads.
    /// </summary>
    public const string AgenticIdentityKey = "AgenticIdentity";

    /// <summary>
    /// Option key under which <see cref="BotAppId"/> is stamped onto the request options.
    /// </summary>
    public const string BotAppIdKey = "botAppId";

    /// <summary>
    /// Gets the agentic identity to authenticate as, when the bot acts on behalf of an agentic app.
    /// </summary>
    public AgenticIdentity? AgenticIdentity { get; init; }

    /// <summary>
    /// Gets the bot application (client) id to mint a token as.
    /// </summary>
    public string? BotAppId { get; init; }

    /// <summary>
    /// Builds context for an <b>outbound</b> activity: the bot is the sender, so the agentic identity and bot app id
    /// are both derived from <see cref="CoreActivity.From"/>.
    /// </summary>
    /// <param name="activity">The outbound activity, or null.</param>
    /// <returns>The context, or null when nothing could be derived.</returns>
    public static BotRequestContext? FromActivity(CoreActivity? activity)
        => Build(Schema.AgenticIdentity.FromAccount(activity?.From), NormalizeAppId(activity?.From?.BotId ?? activity?.From?.Id));

    /// <summary>
    /// Builds context for an <b>inbound</b> activity: the bot is the recipient, so both the bot app id and the
    /// agentic identity are derived from <see cref="CoreActivity.Recipient"/> (the bot's own account).
    /// </summary>
    /// <param name="activity">The inbound activity, or null.</param>
    /// <returns>The context, or null when nothing could be derived.</returns>
    public static BotRequestContext? FromInboundActivity(CoreActivity? activity)
        => Build(Schema.AgenticIdentity.FromAccount(activity?.Recipient), NormalizeAppId(activity?.Recipient?.BotId ?? activity?.Recipient?.Id));

    /// <summary>
    /// Builds context carrying only the supplied agentic identity.
    /// </summary>
    /// <param name="agenticIdentity">The agentic identity, or null.</param>
    /// <returns>The context, or null when <paramref name="agenticIdentity"/> is null.</returns>
    public static BotRequestContext? FromAgenticIdentity(AgenticIdentity? agenticIdentity)
        => agenticIdentity is null ? null : new BotRequestContext { AgenticIdentity = agenticIdentity };

    /// <summary>
    /// Builds context carrying only the supplied bot app id, used as-is (no channel-prefix stripping). Use for
    /// proactive flows where the bot app id is known directly rather than derived from an activity.
    /// </summary>
    /// <param name="botAppId">The bot application (client) id, or null.</param>
    /// <returns>The context, or null when <paramref name="botAppId"/> is null or empty.</returns>
    public static BotRequestContext? FromBotAppId(string? botAppId)
        => string.IsNullOrEmpty(botAppId) ? null : new BotRequestContext { BotAppId = botAppId };

    /// <summary>
    /// Merges two contexts, with non-null fields from <paramref name="overrides"/> taking precedence.
    /// </summary>
    /// <param name="baseContext">The base context, or null.</param>
    /// <param name="overrides">The overriding context, or null.</param>
    /// <returns>The merged context, or null when both inputs are null.</returns>
    public static BotRequestContext? Merge(BotRequestContext? baseContext, BotRequestContext? overrides)
    {
        if (baseContext is null)
        {
            return overrides;
        }

        if (overrides is null)
        {
            return baseContext;
        }

        return new BotRequestContext
        {
            AgenticIdentity = overrides.AgenticIdentity ?? baseContext.AgenticIdentity,
            BotAppId = overrides.BotAppId ?? baseContext.BotAppId,
        };
    }

    /// <summary>
    /// Enumerates the set fields as option key/value pairs to stamp onto the request's options.
    /// </summary>
    internal IEnumerable<KeyValuePair<string, object?>> ToOptions()
    {
        if (AgenticIdentity is not null)
        {
            yield return new KeyValuePair<string, object?>(AgenticIdentityKey, AgenticIdentity);
        }

        if (!string.IsNullOrEmpty(BotAppId))
        {
            yield return new KeyValuePair<string, object?>(BotAppIdKey, BotAppId);
        }
    }

    private static BotRequestContext? Build(AgenticIdentity? agenticIdentity, string? botAppId)
        => agenticIdentity is null && string.IsNullOrEmpty(botAppId)
            ? null
            : new BotRequestContext { AgenticIdentity = agenticIdentity, BotAppId = botAppId };

    // Teams channel accounts carry the bot id as "28:<appId>"; strip the channel prefix when present.
    private static string? NormalizeAppId(string? id)
        => string.IsNullOrEmpty(id)
            ? null
            : id.StartsWith("28:", StringComparison.Ordinal) ? id[3..] : id;
}
