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

// ── Message handler ────────────────────────────────────────────────────────────

teamsApp.OnMessage(async (context, cancellationToken) =>
{
    string conversationId = context.Activity.Conversation?.Id
        ?? throw new InvalidOperationException("Missing conversation ID.");
    string userText = context.Activity.TextWithoutMentions ?? string.Empty;

    TeamsStreamingWriter writer = TeamsStreamingWriter.CreateFromContext(context);
    RunResult result = await agent.RunAsync(conversationId, userText, writer, cancellationToken);

    IList<Entity> entities = result.Citations.BuildEntities(result.FullText);

    List<TeamsAttachment>? attachments = result.PendingCards.Count > 0
        ? [.. result.PendingCards.Select(c => TeamsAttachment.CreateBuilder().WithAdaptiveCard(c).Build())]
        : null;

    SuggestedActions? suggestedActions = result.PendingActions.Count > 0
        ? new SuggestedActions().AddActions([.. result.PendingActions])
        : null;

    await writer.FinalizeResponseAsync(
        attachments: attachments,
        entities: entities.Count > 0 ? entities : null,
        feedbackEnabled: true,
        suggestedActions: suggestedActions,
        cancellationToken: cancellationToken);
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

teamsApp.OnMessageSubmitAction((context, cancellationToken) =>
{
    if (context.Activity.Value?.ActionName == "feedback")
    {
        string? reaction = context.Activity.Value?.ActionValue?["reaction"]?.GetValue<string>();
        string? feedbackText = context.Activity.Value?.ActionValue?["feedbackText"]?.GetValue<string>();
        Console.WriteLine($"Feedback received — reaction: {reaction}, text: {feedbackText}");
    }
    return Task.FromResult(InvokeResponse.Ok());
});

webApp.Run();

// ── Helpers ────────────────────────────────────────────────────────────────────

static TeamsAttachment BuildFeedbackCard(string? reaction)
{
    var submitData = new SubmitActionData();
    submitData.NonSchemaProperties["actionName"] = "feedback";
    submitData.NonSchemaProperties["actionValue"] = new Dictionary<string, object?> { ["reaction"] = reaction ?? "" };

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
            .WithActions(
                new SubmitAction()
                    .WithTitle("Submit")
                    .WithData(new Union<string, SubmitActionData>(submitData))))
        .Build();
}
