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
string[] meterNames = [CoreTelemetryNames.MeterName, TeamsBotApplicationTelemetry.MeterName, "Experimental.Microsoft.Agents.AI", "ModelContextProtocol"];

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
IServiceProvider? rootProvider = null;
builder.Services.AddTeamsBotApplication<ObservabilityBotApp>(o =>
{
    o.UseState();
    o.AddOAuthFlow("sso", flow =>
    {
        flow.OAuthCardText = "Sign in to continue.";
        flow.SignInButtonText = "Sign In";
    });
});
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? throw new InvalidProgramException("Redis connection string not found");
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r
        .AddService(serviceName: "ObservabilityBot", serviceVersion: "0.0.1")
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = builder.Environment.EnvironmentName,
            ["service.namespace"] = "Microsoft.Teams"
        }))
    .UseMicrosoftOpenTelemetry(o =>
    {
        o.Exporters = ExportTarget.Otlp | ExportTarget.AzureMonitor; // | ExportTarget.Agent365
        o.Instrumentation.EnableHttpClientInstrumentation = true;
        o.Instrumentation.EnableAspNetCoreInstrumentation = true;

        /*
        o.Agent365.ContextualTokenResolver = async trctx =>
        {
            IAuthorizationHeaderProvider provider = rootProvider!.GetRequiredService<IAuthorizationHeaderProvider>();
            AuthorizationHeaderProviderOptions options = new() { AcquireTokenOptions = new() { AuthenticationOptionsName = "AzureAd", Tenant = trctx.TenantId } };
            ArgumentNullException.ThrowIfNull(trctx.Identity.AgenticUserId);
            options.WithAgentUserIdentity(trctx.Identity.AgentId, new Guid(trctx.Identity.AgenticUserId));
            var token = await provider.CreateAuthorizationHeaderAsync(
                ["api://9b975845-388f-4429-889e-eab1ef63949c/.default"], options);
            return token.Substring("Bearer".Length).Trim();
        };*/
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
    McpClient msdocsClient = sp.GetRequiredKeyedService<Task<McpClient>>("msdocs").GetAwaiter().GetResult();
    IList<McpClientTool> msdocsTools = msdocsClient.ListToolsAsync().GetAwaiter().GetResult();

    return new ChatOptions
    {
        AllowMultipleToolCalls = true,
        Instructions = "Use the following tools to answer the user's question. If you don't know the answer, use the 'Search Microsoft Docs' tool to find relevant information. Use calendar tools for scheduling-related queries.",
        Tools = [.. msdocsTools]
    };
});

WebApplication app = builder.Build();
rootProvider = app.Services;
app.MapGet("/", () => "ObservabilityBot is running. Telemetry source: " + CoreTelemetryNames.ActivitySourceName);

app.UseTeamsBotApplication<ObservabilityBotApp>();

app.Run();
