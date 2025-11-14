using System.Text.RegularExpressions;
using Microsoft.Teams.Apps.Extensions;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Activities.Invokes;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;
using Microsoft.Teams.AI.Models.OpenAI;
using Microsoft.Teams.AI.Prompts;
using Microsoft.Teams.AI.Templates;
using Azure.AI.OpenAI;
using System.ClientModel;
using Microsoft.Teams.Api.Activities;
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
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Simple chat handler - "hi" command
teamsApp.OnMessage(@"^hi$", async (context) =>
{
    logger.LogInformation($"[COMMAND] 'hi' command invoked by user: {context.Activity.From.Name}");

    var prompt = new OpenAIChatPrompt(aiModel, new ChatPromptOptions
    {
        Instructions = new StringTemplate("You are a friendly assistant who talks like a pirate")
    });

    logger.LogInformation("[COMMAND] Sending 'hi' message to AI with pirate personality...");
    var result = await prompt.Send(context.Activity.Text);
    if (result.Content != null)
    {
        logger.LogInformation($"[COMMAND] AI response: {result.Content}");
        var messageActivity = new MessageActivity
        {
            Text = result.Content,
        }.AddAIGenerated();
        await context.Send(messageActivity);
    }
});

// Pokemon command handler
teamsApp.OnMessage(@"^pokemon\s+(.+)", async (context) =>
{
    logger.LogInformation($"[COMMAND] 'pokemon' command invoked: {context.Activity.Text}");
    var match = Regex.Match(context.Activity.Text ?? "", @"^pokemon\s+(.+)", RegexOptions.IgnoreCase);
    if (match.Success)
    {
        var pokemonName = match.Groups[1].Value.Trim();
        logger.LogInformation($"[COMMAND] Extracted pokemon name: '{pokemonName}'");
        context.Activity.Text = pokemonName;
        await FunctionCallingHandler.HandlePokemonSearch(aiModel, context);
    }
});


// Streaming handler
teamsApp.OnMessage(@"^stream\s+(.+)", async (context) =>
{
    logger.LogInformation($"[COMMAND] 'stream' command invoked: {context.Activity.Text}");
    var match = Regex.Match(context.Activity.Text ?? "", @"^stream\s+(.+)", RegexOptions.IgnoreCase);
    if (match.Success)
    {
        var query = match.Groups[1].Value.Trim();
        logger.LogInformation($"[COMMAND] Extracted query for streaming: '{query}'");
        var prompt = new OpenAIChatPrompt(aiModel, new ChatPromptOptions
        {
            Instructions = new StringTemplate("You are a friendly assistant who responds in extremely verbose language")
        });

        logger.LogInformation("[COMMAND] Sending streaming request to AI...");
        var result = await prompt.Send(query, (chunk) =>
        {
            logger.LogInformation($"[STREAM] Chunk received: {chunk}");
            context.Stream.Emit(chunk);
            return Task.CompletedTask;
        });
    }
});

// Citations handler
teamsApp.OnMessage(@"^citations?\b", async (context) =>
{
    logger.LogInformation($"[COMMAND] 'citations' command invoked: {context.Activity.Text}");
    await CitationsHandler.HandleCitationsDemo(context);
});

// Feedback loop handler
teamsApp.OnMessage(@"^feedback\s+(.+)", async (context) =>
{
    logger.LogInformation($"[COMMAND] 'feedback' command invoked: {context.Activity.Text}");
    var match = Regex.Match(context.Activity.Text ?? "", @"^feedback\s+(.+)", RegexOptions.IgnoreCase);
    if (match.Success)
    {
        var query = match.Groups[1].Value.Trim();
        logger.LogInformation($"[COMMAND] Extracted query for feedback: '{query}'");
        context.Activity.Text = query;
        await FeedbackHandler.HandleFeedbackLoop(aiModel, context);
    }
});

// Memory clear handler
teamsApp.OnMessage(@"^memory\s+clear\b", async (context) =>
{
    logger.LogInformation($"[COMMAND] 'memory clear' command invoked for conversation: {context.Activity.Conversation.Id}");
    await MemoryManagementHandler.ClearConversationMemory(context.Activity.Conversation.Id);
    await context.Reply("🧠 Memory cleared!");
});

// Prompt-based handler (declarative style)
teamsApp.OnMessage(@"^/weather\b", async (context) =>
{
    logger.LogInformation($"[COMMAND] '/weather' command invoked: {context.Activity.Text}");
    var prompt = OpenAIChatPrompt.From(aiModel, new Samples.AI.Prompts.WeatherPrompt(logger));
    var result = await prompt.Send(context.Activity.Text);
    if (!string.IsNullOrEmpty(result.Content))
    {
        logger.LogInformation($"[COMMAND] AI response: {result.Content}");
        var messageActivity = new MessageActivity { Text = result.Content }.AddAIGenerated();
        await context.Send(messageActivity);
    }
    else
    {
        await context.Reply("Sorry I could not figure it out");
    }
});

// Feedback submission handler
teamsApp.OnFeedback((context) =>
{
    logger.LogInformation($"[HANDLER] Feedback submission received");
    FeedbackHandler.HandleFeedbackSubmission(context);
    return Task.CompletedTask;
});

// Fallback stateful conversation handler
teamsApp.OnMessage(async (context) =>
{
    logger.LogInformation($"[FALLBACK] Fallback handler invoked (no command matched): {context.Activity.Text}");
    await MemoryManagementHandler.HandleStatefulConversation(aiModel, context);
});

app.Run();