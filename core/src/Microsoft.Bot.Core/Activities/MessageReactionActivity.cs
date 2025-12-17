// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Core.Activities;

/// <summary>
/// Represents a message reaction activity.
/// </summary>
public class MessageReactionActivity : Activity
{
    /// <summary>
    /// Gets or sets the collection of reactions added.
    /// </summary>
    [JsonPropertyName("reactionsAdded")]
#pragma warning disable CA2227 // Collection properties should be read only
    public IList<MessageReaction>? ReactionsAdded { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

    /// <summary>
    /// Gets or sets the collection of reactions removed.
    /// </summary>
    [JsonPropertyName("reactionsRemoved")]
#pragma warning disable CA2227 // Collection properties should be read only
    public IList<MessageReaction>? ReactionsRemoved { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageReactionActivity"/> class.
    /// </summary>
    public MessageReactionActivity() : base(ActivityTypes.MessageReaction)
    {
    }
}

/// <summary>
/// Represents a message reaction.
/// </summary>
public class MessageReaction
{
    /// <summary>
    /// Gets or sets the type of reaction. See <see cref="ReactionTypes"/> for common values.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of when the user reacted to the message.
    /// </summary>
    [JsonPropertyName("createdDateTime")]
    public string? CreatedDateTime { get; set; }

    /// <summary>
    /// Gets or sets the user with which the reaction is associated.
    /// </summary>
    [JsonPropertyName("user")]
    public ReactionUser? User { get; set; }
}

/// <summary>
/// Represents a user associated with a reaction.
/// </summary>
public class ReactionUser
{
    /// <summary>
    /// Gets or sets the ID of the user.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the identity type of the user. See <see cref="UserIdentityTypes"/> for common values.
    /// </summary>
    [JsonPropertyName("userIdentityType")]
    public string? UserIdentityType { get; set; }

    /// <summary>
    /// Gets or sets the plaintext display name of the user.
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
    /// Azure AD user.
    /// </summary>
    public const string AadUser = "aadUser";

    /// <summary>
    /// On-premise Azure AD user.
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
