// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities;

public partial class ActivityType : StringEnum
{
    public static readonly ActivityType MessageReaction = new("messageReaction");
    public bool IsMessageReaction => MessageReaction.Equals(Value);
}

public class MessageReactionActivity() : Activity(ActivityType.MessageReaction)
{
    [JsonPropertyName("reactionsAdded")]
    [JsonPropertyOrder(121)]
    public IList<Messages.Reaction>? ReactionsAdded { get; set; }

    [JsonPropertyName("reactionsRemoved")]
    [JsonPropertyOrder(122)]
    public IList<Messages.Reaction>? ReactionsRemoved { get; set; }
}