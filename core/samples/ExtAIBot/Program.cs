// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ClientModel;
using Azure.AI.OpenAI;
using ExtAIBot;
using Microsoft.Extensions.AI;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.Handlers.TaskModules;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.Schema.Entities;
using Microsoft.Teams.Cards;
using Microsoft.Teams.Common;

// Wires up the Teams bot application, registers message/feedback handlers,
// and delegates AI execution to Agent.

WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(args);
builder.Services.AddTeamsBotApplication();

string endpoint = builder.Configuration["AzureOpenAI:Endpoint"]
    ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is required.");
string apiKey = builder.Configuration["AzureOpenAI:ApiKey"]
    ?? throw new InvalidOperationException("AzureOpenAI:ApiKey is required.");
string modelId = builder.Configuration["AzureOpenAI:ModelId"]
    ?? throw new InvalidOperationException("AzureOpenAI:ModelId is required.");

IChatClient chatClient = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey))
    .GetChatClient(modelId)
    .AsIChatClient()
    .AsBuilder()
    .UseFunctionInvocation()
    .Build();

McpToolSet mcpTools = await McpToolSet.CreateAsync();
Agent agent = new(chatClient, mcpTools);

WebApplication webApp = builder.Build();
webApp.Lifetime.ApplicationStopping.Register(() => _ = mcpTools.DisposeAsync());

TeamsBotApplication teamsApp = webApp.UseTeamsBotApplication();

// Runs the agent and streams a response back. Shared between the incoming-message
// handler and the clarification-card submit handler — both flows ultimately just
// feed a user-supplied string into the agent.
async Task RespondAsync<TActivity>(Context<TActivity> context, string userText, CancellationToken cancellationToken)
    where TActivity : TeamsActivity
{
    string conversationId = context.Activity.Conversation?.Id
        ?? throw new InvalidOperationException("Missing conversation ID.");

    TeamsStreamingWriter writer = TeamsStreamingWriter.CreateFromContext(context);
    RunResult result = await agent.RunAsync(conversationId, userText, writer, cancellationToken);

    IList<Entity> entities = result.Citations.BuildEntities(result.FullText);

    List<TeamsAttachment>? attachments = result.PendingCards.Count > 0
        ? [.. result.PendingCards.Select(c => TeamsAttachment.CreateBuilder().WithAdaptiveCard(c).Build())]
        : null;

    SuggestedActions? suggestedActions = result.FollowUpActions.Count > 0
        ? new SuggestedActions().AddActions([.. result.FollowUpActions])
        : null;

    // When the agent returns a card (e.g. clarification), send it as an attachment-only
    // reply — no text and no feedback loop, since the card IS the question. Citations and
    // suggested actions still go through.
    //TODO : work on streaming final response API
    await writer.FinalizeResponseAsync(
        attachments: attachments,
        entities: entities,
        feedback: attachments != null ? null : FeedbackType.Custom,
        suggestedActions: suggestedActions,
        text: attachments != null ? "" : null,
        cancellationToken: cancellationToken);
}

// ── Message handler ────────────────────────────────────────────────────────────

teamsApp.OnMessage(async (context, cancellationToken) =>
{
    string userText = context.Activity.TextWithoutMentions ?? "";
    await RespondAsync(context, userText, cancellationToken);
});

// ── Clarification: adaptive card action ───────────────────────────────────────
// Triggered when the user submits the clarification card (Action.Execute, verb "clarification").

teamsApp.OnAdaptiveCardAction(async (context, cancellationToken) =>
{
    if (context.Activity.Value?.Action?.Verb == "clarification")
    {
        string choice = context.Activity.Value.Action.Data?["clarificationChoice"]?.ToString() ?? "";
        await RespondAsync(context, choice, cancellationToken);
    }
    return InvokeResponse.Ok();
});

// ── Feedback: message fetch task ───────────────────────────────────────────────
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

// ── Feedback: message submit action ───────────────────────────────────────────
// Triggered when the user submits the feedback form.

teamsApp.OnMessageSubmitFeedback((context, cancellationToken) =>
{
    MessageSubmitFeedbackValue? feedback = context.Activity.Value;
    Console.WriteLine($"Feedback received — reaction: {feedback?.Reaction}, feedback: {feedback?.Feedback}");
    return Task.FromResult(InvokeResponse.Ok());
});

webApp.Run();

// ── Helpers ────────────────────────────────────────────────────────────────────

static TeamsAttachment BuildFeedbackCard(string? reaction)
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
