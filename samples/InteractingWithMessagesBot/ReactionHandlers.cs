// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Schema;

internal static class ReactionHandlers
{
    internal static void Register(TeamsBotApplication teamsApp)
    {
        teamsApp.OnMessage("(?i)^reaction add (\\S+)$", async (context, cancellationToken) =>
        {
            (string conversationId, string activityId) = GetInboundMessageReference(context.Activity);
            string reactionType = GetReactionType(context.Activity.TextWithoutMentions, "reaction add ");
            await context.Api.Conversations.AddReactionAsync(
                conversationId,
                activityId,
                reactionType,
                cancellationToken: cancellationToken);
        });

        teamsApp.OnMessage("(?i)^reaction remove (\\S+)$", async (context, cancellationToken) =>
        {
            (string conversationId, string activityId) = GetInboundMessageReference(context.Activity);
            string reactionType = GetReactionType(context.Activity.TextWithoutMentions, "reaction remove ");
            await context.Api.Conversations.AddReactionAsync(
                conversationId,
                activityId,
                reactionType,
                cancellationToken: cancellationToken);
            await Task.Delay(2000, cancellationToken);
            await context.Api.Conversations.DeleteReactionAsync(
                conversationId,
                activityId,
                reactionType,
                cancellationToken: cancellationToken);
        });

        teamsApp.OnMessage("(?i)^reaction proactive$", async (context, cancellationToken) =>
        {
            ArgumentNullException.ThrowIfNull(context.Activity.Conversation);
            SendActivityResponse? sent = await teamsApp.SendAsync(
                context.Activity.Conversation.Id,
                "This message was sent and reacted to using app-level APIs.",
                cancellationToken: cancellationToken);
            string activityId = sent?.Id
                ?? throw new InvalidOperationException("SendActivityResponse.Id is required.");
            await context.Api.Conversations.AddReactionAsync(
                context.Activity.Conversation.Id,
                activityId,
                "like",
                cancellationToken: cancellationToken);
        });

        teamsApp.OnMessageReaction(async (context, cancellationToken) =>
        {
            string reactionsAdded = string.Join(", ", context.Activity.ReactionsAdded?.Select(r => r.Type) ?? []);
            string reactionsRemoved = string.Join(", ", context.Activity.ReactionsRemoved?.Select(r => r.Type) ?? []);
            await context.SendAsync(
                $"Reactions added: {reactionsAdded}; reactions removed: {reactionsRemoved}",
                cancellationToken);
        });
    }

    private static (string ConversationId, string ActivityId) GetInboundMessageReference(CoreActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity.Conversation);
        ArgumentException.ThrowIfNullOrEmpty(activity.Id);
        return (activity.Conversation.Id, activity.Id);
    }

    private static string GetReactionType(string? text, string prefix)
    {
        ArgumentException.ThrowIfNullOrEmpty(text);
        return text[prefix.Length..].Trim();
    }
}
