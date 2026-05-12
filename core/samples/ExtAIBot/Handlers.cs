// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.Handlers.TaskModules;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.Schema.Entities;
using Microsoft.Teams.Cards;

namespace ExtAIBot;

// Bot activity handlers: incoming messages, clarification-card submits, and the
// custom-feedback fetch/submit pair. Each handler ultimately funnels a user-supplied
// string back through the Agent.
internal static class Handlers
{
    public static TeamsBotApplication RegisterHandlers(this TeamsBotApplication teamsApp, Agent agent, ILogger logger)
    {
        // Message handler.
        teamsApp.OnMessage(async (context, cancellationToken) =>
        {
            string userText = context.Activity.TextWithoutMentions ?? "";
            await RespondAsync(agent, context, userText, cancellationToken);
        });

        // Clarification: adaptive card action.
        // Triggered when the user submits the clarification card (Action.Execute, verb "clarification").
        teamsApp.OnAdaptiveCardAction(async (context, cancellationToken) =>
        {
            if (context.Activity.Value?.Action?.Verb == "clarification")
            {
                string choice = context.Activity.Value.Action.Data?["clarificationChoice"]?.ToString() ?? "";
                await RespondAsync(agent, context, choice, cancellationToken);
            }
            return InvokeResponse.Ok();
        });

        // Feedback: message fetch task.
        // Triggered when the user clicks thumbs up or thumbs down on a bot reply.
        teamsApp.OnMessageFetchTask((context, cancellationToken) =>
        {
            string? reaction = context.Activity.Value?.Data?.ActionValue?.Reaction;

            return Task.FromResult(TaskModuleResponse.CreateBuilder()
                .WithType(TaskModuleResponseType.Continue)
                .WithTitle("Feedback")
                .WithHeight(TaskModuleSize.Small)
                .WithWidth(TaskModuleSize.Small)
                .WithCard(BuildFeedbackCard(reaction))
                .Build());
        });

        // Feedback: message submit action.
        teamsApp.OnMessageSubmitFeedback((context, cancellationToken) =>
        {
            MessageSubmitFeedbackValue? feedback = context.Activity.Value;
            logger.LogInformation("Feedback received — reaction: {Reaction}, feedback: {Feedback}",
                feedback?.Reaction, feedback?.Feedback);
            return Task.FromResult(InvokeResponse.Ok());
        });

        return teamsApp;
    }

    // Runs the agent and streams a response back. Shared between the incoming-message
    // handler and the clarification-card submit handler — both flows ultimately just
    // feed a user-supplied string into the agent.
    private static async Task RespondAsync<TActivity>(Agent agent, Context<TActivity> context, string userText, CancellationToken cancellationToken)
        where TActivity : TeamsActivity
    {
        _ = context.Activity.Conversation?.Id
            ?? throw new InvalidOperationException("Missing conversation ID.");

        TeamsStreamingWriter writer = TeamsStreamingWriter.CreateFromContext(context);
        RunResult result = await agent.RunAsync(context.Activity.Conversation!.Id, userText, writer, cancellationToken);

        IList<Entity> entities = result.Citations.BuildEntities(result.FullText);

        MessageActivity final = new();

        if (result.PendingCards.Count > 0)
        {
            // Card-only reply (e.g. clarification). No text and no feedback — the card IS the question.
            final.Text = "";
            final.AddAttachment([.. result.PendingCards.Select(c =>
                TeamsAttachment.CreateBuilder().WithAdaptiveCard(c).Build())]);
        }
        else
        {
            final.AddFeedback(FeedbackType.Custom);
        }

        foreach (Entity entity in entities) final.AddEntity(entity);

        if (result.FollowUpActions.Count > 0)
            final.WithSuggestedActions(new SuggestedActions().AddActions([.. result.FollowUpActions]));

        await writer.FinalizeResponseAsync(final, cancellationToken);
    }

    private static TeamsAttachment BuildFeedbackCard(string? reaction)
    {
        return TeamsAttachment.CreateBuilder()
            .WithAdaptiveCard(new AdaptiveCard(
                new TextBlock(reaction is null
                    ? "Tell us more about your experience:"
                    : $"You clicked {reaction}. Tell us more:")
                    .WithWrap(true),
                new TextInput()
                    .WithId("feedbackText")
                    .WithPlaceholder("Enter your feedback here...")
                    .WithIsMultiline(true))
                .WithActions(new SubmitAction().WithTitle("Submit")))
            .Build();
    }
}
