using System.ClientModel;
using System.Text.RegularExpressions;

using Azure.AI.OpenAI;

using Microsoft.Teams.AI.Models.OpenAI;
using Microsoft.Teams.AI.Prompts;
using Microsoft.Teams.AI.Templates;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Activities.Invokes;
using Microsoft.Teams.Apps.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

using Samples.AI.Handlers;

var builder = WebApplication.CreateBuilder(args);

// Configuration
var azureOpenAIModel = builder.Configuration["AzureOpenAIModel"] ?? throw new InvalidOperationException("AzureOpenAIModel not configured");
var azureOpenAIEndpoint = builder.Configuration["AzureOpenAIEndpoint"] ?? throw new InvalidOperationException("AzureOpenAIEndpoint not configured");
var azureOpenAIKey = builder.Configuration["AzureOpenAIKey"] ?? throw new InvalidOperationException("AzureOpenAIKey not configured");

var azureOpenAI = new AzureOpenAIClient(
    new Uri(azureOpenAIEndpoint),
    new ApiKeyCredential(azureOpenAIKey)
);

// Register AI Model as singleton
var aiModel = new OpenAIChatModel(azureOpenAIModel, azureOpenAI);

builder.AddTeams().AddTeamsDevTools();
var app = builder.Build();

var teamsApp = app.UseTeams();

// Simple chat handler - "hi" command
teamsApp.OnMessage(@"^hi$", async (context, cancellationToken) =>
{
    context.Log.Info($"[COMMAND] 'hi' command invoked by user: {context.Activity.From.Name}");

    var prompt = new OpenAIChatPrompt(aiModel, new ChatPromptOptions
    {
        Instructions = new StringTemplate("You are a friendly assistant who talks like a pirate")
    });

    context.Log.Info("[COMMAND] Sending 'hi' message to AI with pirate personality...");
    var result = await prompt.Send(context.Activity.Text, cancellationToken);
    if (result.Content != null)
    {
        context.Log.Info($"[COMMAND] AI response: {result.Content}");
        var messageActivity = new MessageActivity
        {
            Text = result.Content,
        }.AddAIGenerated();
        await context.Send(messageActivity, cancellationToken);
    }
});

// Pokemon command handler
teamsApp.OnMessage(@"^pokemon\s+(.+)", async (context, cancellationToken) =>
{
    context.Log.Info($"[COMMAND] 'pokemon' command invoked: {context.Activity.Text}");
    var match = Regex.Match(context.Activity.Text ?? "", @"^pokemon\s+(.+)", RegexOptions.IgnoreCase);
    if (match.Success)
    {
        var pokemonName = match.Groups[1].Value.Trim();
        context.Log.Info($"[COMMAND] Extracted pokemon name: '{pokemonName}'");
        context.Activity.Text = pokemonName;
        await FunctionCallingHandler.HandlePokemonSearch(aiModel, context, cancellationToken);
    }
});


// Streaming handler
teamsApp.OnMessage(@"^stream\s+(.+)", async (context, cancellationToken) =>
{
    context.Log.Info($"[COMMAND] 'stream' command invoked: {context.Activity.Text}");
    var match = Regex.Match(context.Activity.Text ?? "", @"^stream\s+(.+)", RegexOptions.IgnoreCase);
    if (match.Success)
    {
        var query = match.Groups[1].Value.Trim();
        context.Log.Info($"[COMMAND] Extracted query for streaming: '{query}'");
        var prompt = new OpenAIChatPrompt(aiModel, new ChatPromptOptions
        {
            Instructions = new StringTemplate("You are a friendly assistant who responds in extremely verbose language")
        });

        context.Log.Info("[COMMAND] Sending streaming request to AI...");
        var result = await prompt.Send(query, (chunk) =>
        {
            context.Log.Info($"[STREAM] Chunk received: {chunk}");
            context.Stream.Emit(chunk);
            return Task.CompletedTask;
        }, cancellationToken);
    }
});

// Citations handler
teamsApp.OnMessage(@"^citations?\b", async (context, cancellationToken) =>
{
    context.Log.Info($"[COMMAND] 'citations' command invoked: {context.Activity.Text}");
    await CitationsHandler.HandleCitationsDemo(context, cancellationToken);
});

// Feedback loop handler
teamsApp.OnMessage(@"^feedback\s+(.+)", async (context, cancellationToken) =>
{
    context.Log.Info($"[COMMAND] 'feedback' command invoked: {context.Activity.Text}");
    var match = Regex.Match(context.Activity.Text ?? "", @"^feedback\s+(.+)", RegexOptions.IgnoreCase);
    if (match.Success)
    {
        var query = match.Groups[1].Value.Trim();
        context.Log.Info($"[COMMAND] Extracted query for feedback: '{query}'");
        context.Activity.Text = query;
        await FeedbackHandler.HandleFeedbackLoop(aiModel, context, cancellationToken);
    }
});

// Memory clear handler
teamsApp.OnMessage(@"^memory\s+clear\b", async (context, cancellationToken) =>
{
    context.Log.Info($"[COMMAND] 'memory clear' command invoked for conversation: {context.Activity.Conversation.Id}");
    await MemoryManagementHandler.ClearConversationMemory(context.Activity.Conversation.Id);
    await context.Reply("🧠 Memory cleared!", cancellationToken);
});

// Prompt-based handler (declarative style)
teamsApp.OnMessage(@"^/weather\b", async (context, cancellationToken) =>
{
    context.Log.Info($"[COMMAND] '/weather' command invoked: {context.Activity.Text}");
    var prompt = OpenAIChatPrompt.From(aiModel, new Samples.AI.Prompts.WeatherPrompt(context.ToActivityType()));
    var result = await prompt.Send(context.Activity.Text, cancellationToken);
    if (!string.IsNullOrEmpty(result.Content))
    {
        context.Log.Info($"[COMMAND] AI response: {result.Content}");
        var messageActivity = new MessageActivity { Text = result.Content }.AddAIGenerated();
        await context.Send(messageActivity, cancellationToken);
    }
    else
    {
        await context.Reply("Sorry I could not figure it out", cancellationToken);
    }
});

// Feedback submission handler
teamsApp.OnFeedback((context, cancellationToken) =>
{
    context.Log.Info($"[HANDLER] Feedback submission received");
    FeedbackHandler.HandleFeedbackSubmission(context);
    return Task.CompletedTask;
});

// Fallback stateful conversation handler
teamsApp.OnMessage(async (context, cancellationToken) =>
{
    context.Log.Info($"[FALLBACK] Fallback handler invoked (no command matched): {context.Activity.Text}");
    await MemoryManagementHandler.HandleStatefulConversation(aiModel, context, cancellationToken);
});

app.Run();