// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Api.Clients;
using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.Handlers.TaskModules;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.Schema.Entities;
using Microsoft.Teams.Cards;

namespace ExtAIBot;

// Teams bot subclass: wires the four activity handlers (message, clarification card
// submit, custom-feedback fetch/submit) in its constructor. Each handler funnels a
// user-supplied string back through the Agent.
internal class ExtAIBotApp : TeamsBotApplication
{
    private readonly Agent _agent;
    private readonly ILogger<ExtAIBotApp> _logger;

    public ExtAIBotApp(
        Agent agent,
        ApiClient api,
        IHttpContextAccessor accessor,
        ILogger<ExtAIBotApp> logger,
        TeamsBotApplicationOptions? options = null)
        : base(api, accessor, logger, options)
    {
        _agent = agent;
        _logger = logger;

        // Message handler.
        this.OnMessage(async (context, cancellationToken) =>
        {
            string userText = context.Activity.TextWithoutMentions ?? "";
            await RespondAsync(context, userText, cancellationToken);
        });

        // Clarification: adaptive card action.
        // Triggered when the user submits the clarification card (Action.Execute, verb "clarification").
        this.OnAdaptiveCardAction(async (context, cancellationToken) =>
        {
            if (context.Activity.Value?.Action?.Verb == "clarification")
            {
                string choice = context.Activity.Value.Action.Data?["clarificationChoice"]?.ToString() ?? "";
                await RespondAsync(context, choice, cancellationToken);
            }
            return InvokeResponse.Ok();
        });

        // Feedback: message fetch task.
        // Triggered when the user clicks thumbs up or thumbs down on a bot reply.
        this.OnMessageFetchTask((context, cancellationToken) =>
        {
            string? reaction = context.Activity.Value?.Data?.ActionValue?.Reaction;

            return Task.FromResult(TaskModuleResponse.CreateBuilder()
                .WithType(TaskModuleResponseTypes.Continue)
                .WithTitle("Feedback")
                .WithHeight(TaskModuleSizes.Small)
                .WithWidth(TaskModuleSizes.Small)
                .WithCard(BuildFeedbackCard(reaction))
                .Build());
        });

        // Feedback: message submit action.
        this.OnMessageSubmitFeedback((context, cancellationToken) =>
        {
            MessageSubmitFeedbackValue? feedback = context.Activity.Value;
            _logger.LogInformation("Feedback received — reaction: {Reaction}, feedback: {Feedback}",
                feedback?.Reaction, feedback?.Feedback);
            return Task.FromResult(InvokeResponse.Ok());
        });
    }

    // Runs the agent and streams a response back. Shared between the incoming-message
    // handler and the clarification-card submit handler — both flows ultimately just
    // feed a user-supplied string into the agent.
    private async Task RespondAsync<TActivity>(Context<TActivity> context, string userText, CancellationToken cancellationToken)
        where TActivity : TeamsActivity
    {
        _ = context.Activity.Conversation?.Id
            ?? throw new InvalidOperationException("Missing conversation ID.");

        TeamsStreamingWriter writer = TeamsStreamingWriter.CreateFromContext(context);
        RunResult result = await _agent.RunAsync(context.Activity.Conversation!.Id, userText, writer, cancellationToken);

        IList<Entity> entities = result.Citations.BuildEntities(result.FullText);

        MessageActivityBuilder finalBuilder = MessageActivity.CreateBuilder();

        if (result.PendingCards.Count > 0)
        {
            // Card-only reply (e.g. clarification). No text and no feedback — the card IS the question.
            finalBuilder.WithText("")
                .AddAttachment([.. result.PendingCards.Select(c =>
                    TeamsAttachment.CreateBuilder().WithAdaptiveCard(c).Build())]);
        }
        else
        {
            finalBuilder.AddFeedback(FeedbackTypes.Custom);
        }

        foreach (Entity entity in entities) finalBuilder.AddEntity(entity);

        if (result.FollowUpActions.Count > 0)
            finalBuilder.WithSuggestedActions(new SuggestedActions().AddActions([.. result.FollowUpActions]));

        MessageActivity final = finalBuilder.Build();

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
