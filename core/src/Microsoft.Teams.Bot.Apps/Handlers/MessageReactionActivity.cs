// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.Handlers;

/// <summary>
/// Represents a message reaction activity.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227: Collection Properties should be read only", Justification = "<Pending>")]
public class MessageReactionActivity : TeamsActivity
{
    /// <summary>
    /// Convenience method to create a MessageReactionActivity from a CoreActivity.
    /// </summary>
    /// <param name="activity">The CoreActivity to convert.</param>
    /// <returns>A MessageReactionActivity instance.</returns>
    public static new MessageReactionActivity FromActivity(CoreActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return new MessageReactionActivity(activity);
    }

    /// <summary>
    /// Deserializes a JSON string into a MessageReactionActivity instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A MessageReactionActivity instance.</returns>
    public static new MessageReactionActivity FromJsonString(string json)
    {
        return FromJsonString(json, TeamsActivityJsonContext.Default.MessageReactionActivity);
    }

    /// <summary>
    /// Serializes the MessageReactionActivity to JSON with all message reaction-specific properties.
    /// </summary>
    /// <returns>JSON string representation of the MessageReactionActivity</returns>
    public override string ToJson()
        => ToJson(TeamsActivityJsonContext.Default.MessageReactionActivity);

    /// <summary>
    /// Default constructor.
    /// </summary>
    [JsonConstructor]
    public MessageReactionActivity() : base(TeamsActivityType.MessageReaction)
    {
    }

    /// <summary>
    /// Internal constructor to create MessageReactionActivity from CoreActivity.
    /// </summary>
    /// <param name="activity">The CoreActivity to convert.</param>
    protected MessageReactionActivity(CoreActivity activity) : base(activity)
    {
        if (activity.Properties.TryGetValue("reactionsAdded", out var reactionsAdded) && reactionsAdded != null)
        {
            if (reactionsAdded is JsonElement je)
            {
                ReactionsAdded = JsonSerializer.Deserialize<List<MessageReaction>>(je.GetRawText());
            }
            else
            {
                ReactionsAdded = reactionsAdded as IList<MessageReaction>;
            }
        }
        if (activity.Properties.TryGetValue("reactionsRemoved", out var reactionsRemoved) && reactionsRemoved != null)
        {
            if (reactionsRemoved is JsonElement je)
            {
                ReactionsRemoved = JsonSerializer.Deserialize<List<MessageReaction>>(je.GetRawText());
            }
            else
            {
                ReactionsRemoved = reactionsRemoved as IList<MessageReaction>;
            }
        }
    }

    /// <summary>
    /// Gets or sets the reactions added to the message.
    /// </summary>
    [JsonPropertyName("reactionsAdded")]
    public IList<MessageReaction>? ReactionsAdded { get; set; }

    /// <summary>
    /// Gets or sets the reactions removed from the message.
    /// </summary>
    [JsonPropertyName("reactionsRemoved")]
    public IList<MessageReaction>? ReactionsRemoved { get; set; }
}

/// <summary>
/// Represents a reaction to a message.
/// </summary>
public class MessageReaction
{
    /// <summary>
    /// Gets or sets the type of reaction.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the reaction was created.
    /// </summary>
    [JsonPropertyName("createdDateTime")]
    public DateTime? CreatedDateTime { get; set; }

    /// <summary>
    /// Gets or sets the user who created the reaction.
    /// </summary>
    [JsonPropertyName("user")]
    public ReactionUser? User { get; set; }
}

/// <summary>
/// Represents a user who created a reaction.
/// </summary>
public class ReactionUser
{
    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the user identity type.
    /// </summary>
    [JsonPropertyName("userIdentityType")]
    public string? UserIdentityType { get; set; }

    /// <summary>
    /// Gets or sets the display name of the user.
    /// </summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }
}

/// <summary>
/// String constants for reaction types.
/// </summary>
public static class ReactionTypes
{
    /// <summary>
    /// Like reaction.
    /// </summary>
    public const string Like = "like";

    /// <summary>
    /// Heart reaction.
    /// </summary>
    public const string Heart = "heart";

    /// <summary>
    /// Laugh reaction.
    /// </summary>
    public const string Laugh = "laugh";

    /// <summary>
    /// Surprise reaction.
    /// </summary>
    public const string Surprise = "surprise";

    /// <summary>
    /// Sad reaction.
    /// </summary>
    public const string Sad = "sad";

    /// <summary>
    /// Angry reaction.
    /// </summary>
    public const string Angry = "angry";

    /// <summary>
    /// Plus one reaction.
    /// </summary>
    public const string PlusOne = "plusOne";
}

/// <summary>
/// String constants for user identity types.
/// </summary>
public static class UserIdentityTypes
{
    /// <summary>
    /// Azure Active Directory user.
    /// </summary>
    public const string AadUser = "aadUser";

    /// <summary>
    /// On-premise Azure Active Directory user.
    /// </summary>
    public const string OnPremiseAadUser = "onPremiseAadUser";

    /// <summary>
    /// Anonymous guest user.
    /// </summary>
    public const string AnonymousGuest = "anonymousGuest";

    /// <summary>
    /// Federated user.
    /// </summary>
    public const string FederatedUser = "federatedUser";
}
