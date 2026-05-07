// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Text.Json;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.OpenTelemetry;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Diagnostics;
using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.Schema.Entities;
using Microsoft.Teams.Core.Diagnostics;
using ModelContextProtocol.Client;
using OpenTelemetry;
using OpenTelemetry.Resources;


string[] activitySources = [CoreTelemetryNames.ActivitySourceName, TeamsBotApplicationTelemetry.ActivitySourceName, "Experimental.Microsoft.Agents.AI", "ModelContextProtocol"];
string[] meterNames      = [CoreTelemetryNames.MeterName, TeamsBotApplicationTelemetry.MeterName, "Experimental.Microsoft.Agents.AI", "ModelContextProtocol"];

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
IServiceProvider? rootProvider = null;
builder.Services.AddTeamsBotApplication();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r
        .AddService(serviceName: "ObservabilityBot", serviceVersion: "0.0.1")
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = builder.Environment.EnvironmentName,
            ["service.namespace"] = "Microsoft.Teams"
        }))
    .UseMicrosoftOpenTelemetry(o => {
        o.Instrumentation.EnableHttpClientInstrumentation = true;
        o.Exporters = ExportTarget.Otlp | ExportTarget.Agent365 | ExportTarget.AzureMonitor;
        o.Instrumentation.EnableAspNetCoreInstrumentation = true;
        o.Agent365.Exporter.UseS2SEndpoint = true;
        o.Agent365.Exporter.TokenResolver = async (agentId, tenantId) =>
        {
            var provider = rootProvider!.GetRequiredService<IAuthorizationHeaderProvider>();
            var options = new AuthorizationHeaderProviderOptions { AcquireTokenOptions = new() { AuthenticationOptionsName = "AzureAd", Tenant = tenantId } };
            options.WithAgentIdentity(agentId);
            var token = await provider.CreateAuthorizationHeaderForAppAsync(
                "api://9b975845-388f-4429-889e-eab1ef63949c/.default", options);
            return token.Substring("Bearer".Length).Trim();
        };
     })
    .WithTracing(t => t.AddSource(activitySources))
    .WithMetrics(m => m.AddMeter(meterNames));

builder.Logging.AddOpenTelemetry(o => o.IncludeFormattedMessage = true);


var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new InvalidDataException("AZURE_OPENAI_ENDPOINT not found");
var azoai_key = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY") ?? throw new InvalidDataException("AZURE_OPENAI_KEY not found");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT") ?? throw new InvalidDataException("AZURE_OPENAI_DEPLOYMENT not found");


IChatClient client =
    new ChatClientBuilder(
        new AzureOpenAIClient(new Uri(endpoint), new System.ClientModel.ApiKeyCredential(azoai_key))
            .GetChatClient(deploymentName)
            .AsIChatClient())
    .UseFunctionInvocation()
    .UseOpenTelemetry(sourceName: "Experimental.Microsoft.Extensions.AI")
    .UseLogging(LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information)))
    .Build();

var mcpClient = await McpClient.CreateAsync(
    new HttpClientTransport(new()
    {
        Endpoint = new Uri("https://learn.microsoft.com/api/mcp"),
        TransportMode = HttpTransportMode.AutoDetect,
        Name = "msdocs"
    }));

var tools = await mcpClient.ListToolsAsync();
Console.WriteLine("Tools Found: " + string.Join(", ", tools.Select(t => t.Name)));

var chatOptions = new ChatOptions
{
    AllowMultipleToolCalls = true,
    Instructions = "Use the following tools to answer the user's question. If you don't know the answer, use the 'Search Microsoft Docs' tool to find relevant information.",
    Tools = [.. tools]
};


WebApplication app = builder.Build();
rootProvider = app.Services;
app.MapGet("/", () => "ObservabilityBot is running. Telemetry source: " + CoreTelemetryNames.ActivitySourceName);

var teamsApp = app.UseTeamsBotApplication();

var chatHistories = new ConcurrentDictionary<string, List<ChatMessage>>();
teamsApp.OnMessage(async (context, ct) =>
{
    ArgumentNullException.ThrowIfNull(context.Activity);
    ArgumentNullException.ThrowIfNull(context.Activity.Conversation);
    ArgumentNullException.ThrowIfNull(context.Activity.Conversation.Id);

    await context.Typing(string.Empty, ct);

    var conversationId = context.Activity.Conversation.Id;
    var history = chatHistories.GetOrAdd(conversationId, _ => []);

    lock (history)
    {
        history.Add(new ChatMessage(ChatRole.User, context.Activity.Text));
    }

    var (responseText, citations) = await GetChatResponseAsync(history);

    var responseMsg = TeamsActivity.CreateBuilder()
        .WithText(responseText, TextFormats.Markdown)
        .AddMention(context.Activity?.From!)
        .Build();

    responseMsg.AddAIGenerated();

    for (int i = 0; i < citations.Count; i++)
    {
        var citation = citations[i];
        var abstract_ = citation.Content.Length > 400 ? citation.Content[..200] + "..." : citation.Content;
        responseMsg.AddCitation(i + 1, new CitationAppearance() { Name = citation.Title, Url = new Uri(citation.Url), Abstract = abstract_, Icon = CitationIcon.Text });
    }

    await context.Send(responseMsg, ct);
});

app.Run();

async Task<(string ResponseText, List<(string Title, string Url, string Content)> Citations)> GetChatResponseAsync(List<ChatMessage> history)
{
    List<ChatMessage> snapshot;
    lock (history)
    {
        snapshot = [.. history];
    }

    ChatResponse response = await client.GetResponseAsync(snapshot, chatOptions);

    lock (history)
    {
        history.AddRange(response.Messages);
    }

    var toolsUsed = response.Messages.SelectMany(m => m.Contents.OfType<FunctionCallContent>());
    Console.WriteLine("Tools used " + toolsUsed.Count());

    var citations = response.Messages
        .SelectMany(m => m.Contents.OfType<FunctionResultContent>())
        .Where(frc => frc.Result is not null)
        .SelectMany(frc =>
        {
            try
            {
                var json = JsonSerializer.Deserialize<JsonElement>(frc.Result!.ToString()!);
                if (json.TryGetProperty("structuredContent", out var sc) &&
                    sc.TryGetProperty("results", out var results))
                {
                    return results.EnumerateArray()
                        .Where(r => r.TryGetProperty("contentUrl", out _))
                        .Select(r => (
                            Title: r.GetProperty("title").GetString() ?? "",
                            Url: r.GetProperty("contentUrl").GetString() ?? "",
                            Content: r.TryGetProperty("content", out var c) ? c.GetString() ?? "" : ""
                        ));
                }
            }
            catch { }
            return [];
        })
        .DistinctBy(c => c.Url)
        .Take(5).ToList();

    var responseText = response.Text;

    for (int i = 1; i < citations.Count; i++)
    {
        responseText += $"[{i}] ";
    }

    return (responseText, citations);
}
