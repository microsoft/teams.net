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

    public MessageReactionActivity AddReaction(Messages.Reaction reaction)
    {
        ReactionsAdded ??= [];
        ReactionsAdded.Add(reaction);
        return this;
    }

    public MessageReactionActivity AddReaction(Messages.ReactionType type)
    {
        ReactionsAdded ??= [];
        ReactionsAdded.Add(new() { Type = type });
        return this;
    }

    public MessageReactionActivity RemoveReaction(Messages.Reaction reaction)
    {
        ReactionsRemoved ??= [];

        if (ReactionsAdded is not null)
        {
            var i = ReactionsAdded.ToList().FindIndex(r =>
                r.Type.Equals(reaction.Type) && r.User?.Id == reaction.User?.Id
            );

            if (i > -1)
            {
                ReactionsAdded.RemoveAt(i);
                return this;
            }
        }

        ReactionsRemoved.Add(reaction);
        return this;
    }

    public MessageReactionActivity RemoveReaction(Messages.ReactionType type)
    {
        ReactionsRemoved ??= [];

        if (ReactionsAdded is not null)
        {
            var i = ReactionsAdded.ToList().FindIndex(r => r.Type.Equals(type));

            if (i > -1)
            {
                ReactionsAdded.RemoveAt(i);
                return this;
            }
        }

        ReactionsRemoved.Add(new() { Type = type });
        return this;
    }
}