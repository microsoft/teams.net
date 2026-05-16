// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.OpenTelemetry;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Diagnostics;
using Microsoft.Teams.Core.Diagnostics;
using ModelContextProtocol.Client;
using ObservabilityBot;
using OpenTelemetry;
using OpenTelemetry.Resources;


string[] activitySources = [CoreTelemetryNames.ActivitySourceName, TeamsBotApplicationTelemetry.ActivitySourceName, "Experimental.Microsoft.Extensions.AI", "ModelContextProtocol"];
string[] meterNames      = [CoreTelemetryNames.MeterName, TeamsBotApplicationTelemetry.MeterName, "Experimental.Microsoft.Agents.AI", "ModelContextProtocol"];

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
IServiceProvider? rootProvider = null;
builder.Services.AddTeamsBotApplication<ObservabilityBotApp>();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r
        .AddService(serviceName: "ObservabilityBot", serviceVersion: "0.0.1")
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = builder.Environment.EnvironmentName,
            ["service.namespace"] = "Microsoft.Teams"
        }))
    .UseMicrosoftOpenTelemetry(o => {
        o.Exporters = ExportTarget.Otlp | ExportTarget.Agent365 | ExportTarget.AzureMonitor;
        o.Instrumentation.EnableHttpClientInstrumentation = true;
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

// Register MCP clients
builder.Services.AddKeyedSingleton("msdocs", (sp, key) =>
    McpClient.CreateAsync(
        new HttpClientTransport(new()
        {
            Endpoint = new Uri("https://learn.microsoft.com/api/mcp"),
            TransportMode = HttpTransportMode.AutoDetect,
            Name = "msdocs"
        })));

// Register IChatClient
var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new InvalidDataException("AZURE_OPENAI_ENDPOINT not found");
var azoai_key = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY") ?? throw new InvalidDataException("AZURE_OPENAI_KEY not found");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT") ?? throw new InvalidDataException("AZURE_OPENAI_DEPLOYMENT not found");

builder.Services.AddSingleton<IChatClient>(sp =>
    new ChatClientBuilder(
        new AzureOpenAIClient(new Uri(endpoint), new System.ClientModel.ApiKeyCredential(azoai_key))
            .GetChatClient(deploymentName)
            .AsIChatClient())
    .UseFunctionInvocation()
    .UseOpenTelemetry(sourceName: "Experimental.Microsoft.Extensions.AI")
    .UseLogging(sp.GetRequiredService<ILoggerFactory>())
    .Build());

builder.Services.AddSingleton<ChatOptions>(sp =>
{
    var msdocsClient = sp.GetRequiredKeyedService<Task<McpClient>>("msdocs").GetAwaiter().GetResult();
    var msdocsTools = msdocsClient.ListToolsAsync().GetAwaiter().GetResult();

    return new ChatOptions
    {
        AllowMultipleToolCalls = true,
        Instructions = "Use the following tools to answer the user's question. If you don't know the answer, use the 'Search Microsoft Docs' tool to find relevant information. Use calendar tools for scheduling-related queries.",
        Tools = [..msdocsTools]
    };
});

WebApplication app = builder.Build();
rootProvider = app.Services;
app.MapGet("/", () => "ObservabilityBot is running. Telemetry source: " + CoreTelemetryNames.ActivitySourceName);

app.UseTeamsBotApplication<ObservabilityBotApp>();

app.Run();
