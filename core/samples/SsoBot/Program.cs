// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// This sample demonstrates Teams SSO using the context-level API with a single OAuth connection.
// The context API is the simplest way to add authentication -- when only one OAuthFlow is registered,
// Resolve the flow once via bot.GetOAuthFlow("sso") and call SignInAsync/SignOutAsync on it.
//
// Azure Bot resource must have one OAuth connection setting configured:
// | Connection name   | Provider    | Scopes                   |
// |-------------------|-------------|--------------------------|
// | GraphConnection   | Azure AD v2 | User.Read Calendars.Read |

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.OpenTelemetry;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Diagnostics;
using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.OAuth;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Resources;

WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);

string[] activitySources = [CoreTelemetryNames.ActivitySourceName, TeamsBotApplicationTelemetry.ActivitySourceName];
string[] meterNames = [CoreTelemetryNames.MeterName, TeamsBotApplicationTelemetry.MeterName];

webAppBuilder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r
        .AddService(serviceName: "SsoBot", serviceVersion: "0.0.1")
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = webAppBuilder.Environment.EnvironmentName,
            ["service.namespace"] = "Microsoft.Teams"
        }))
    .UseMicrosoftOpenTelemetry(o =>
    {
        o.Exporters = ExportTarget.Otlp;
        o.Instrumentation.EnableHttpClientInstrumentation = true;
        o.Instrumentation.EnableAspNetCoreInstrumentation = true;
    })
    .WithTracing(t => t.AddSource(activitySources))
    .WithMetrics(m => m.AddMeter(meterNames));

// Configure the single OAuth flow at the DI level
webAppBuilder.Services.AddTeamsBotApplication(options =>
{
    options.AddOAuthFlow("sso");
});

WebApplication webApp = webAppBuilder.Build();

TeamsBotApplication bot = webApp.UseTeamsBotApplication();

// Get the pre-registered flow and attach callbacks
OAuthFlow auth = bot.GetOAuthFlow("sso");

auth.OnSignInComplete(async (context, tokenResponse, ct) =>
{
    await context.SendActivityAsync("You're now signed in! Try `profile` or `calendar`.", ct);
});

auth.OnSignInFailure(async (context, failure, ct) =>
{
    string message = failure is not null
        ? $"Sign-in failed: {failure.Code} — {failure.Message}"
        : "Sign-in failed. Please try again.";
    await context.SendActivityAsync(message, ct);
});

// ==================== MESSAGE HANDLERS ====================

bot.OnMessage("(?i)^login$", async (context, ct) =>
{
    // auth is the pre-resolved OAuthFlow (bot.GetOAuthFlow("sso"))
    string? token = await auth.SignInAsync(context, ct);
    if (token is not null)
    {
        await context.SendActivityAsync("You're already signed in.", ct);
    }
    // else: OAuthCard sent, SSO flow in progress -- OnSignInComplete will fire
});

bot.OnMessage("(?i)^profile$", async (context, ct) =>
{
    // SignIn doubles as "get token if cached, else start sign-in"
    string? token = await auth.SignInAsync(context, ct);
    if (token is null) return; // sign-in card sent, wait for completion

    using HttpClient http = new();
    http.DefaultRequestHeaders.Authorization = new("Bearer", token);

    try
    {
        string json = await http.GetStringAsync("https://graph.microsoft.com/v1.0/me", ct);
        string indentedJson = JsonSerializer.Serialize(JsonSerializer.Deserialize<JsonObject>(json), new JsonSerializerOptions { WriteIndented = true });
        await context.SendActivityAsync(MessageActivityInput.CreateBuilder().WithText($" ## Graph Me \n ```json\n{indentedJson}\n```", TextFormats.Markdown).Build(), ct);
    }
    catch (HttpRequestException ex)
    {
        await context.SendActivityAsync($"Graph call failed: {ex.Message}", ct);
    }
});

bot.OnMessage("(?i)^calendar$", async (context, ct) =>
{
    string? token = await auth.SignInAsync(context, ct);
    if (token is null) return;

    using HttpClient http = new();
    http.DefaultRequestHeaders.Authorization = new("Bearer", token);

    try
    {
        string json = await http.GetStringAsync(
            "https://graph.microsoft.com/v1.0/me/events?$top=3&$select=subject,start,end&$orderby=start/dateTime", ct);
        string indentedJson = JsonSerializer.Serialize(JsonSerializer.Deserialize<JsonObject>(json), new JsonSerializerOptions { WriteIndented = true });
        await context.SendActivityAsync(MessageActivityInput.CreateBuilder().WithText($" ## Graph Calendar \n ```json\n{indentedJson}\n```", TextFormats.Markdown).Build(), ct);
    }
    catch (HttpRequestException ex)
    {
        await context.SendActivityAsync($"Graph call failed: {ex.Message}", ct);
    }
});

bot.OnMessage("(?i)^logout$", async (context, ct) =>
{
    await auth.SignOutAsync(context, ct);
    await context.SendActivityAsync("Signed out.", ct);
});

bot.OnMessage("(?i)^status$", async (context, ct) =>
{
    bool signedIn = await auth.IsSignedInAsync(context, ct);
    await context.SendActivityAsync(signedIn ? "Signed in." : "Not signed in.", ct);
});

bot.OnMessage("(?i)^help$", async (context, ct) =>
{
    string helpText = """
        **SSO Bot** - Single-connection SSO sample

        Commands:
        - `login` - Sign in with SSO
        - `profile` - Get your Azure AD profile (signs in if needed)
        - `calendar` - Get your next 3 calendar events (signs in if needed)
        - `status` - Check sign-in status
        - `logout` - Sign out
        - `help` - Show this message
        """;

    await context.SendActivityAsync(
        MessageActivityInput.CreateBuilder().WithText(helpText, TextFormats.Markdown).Build(), ct);
});

// ==================== INSTALL HANDLER ====================

bot.OnInstall(async (context, ct) =>
{
    await context.SendActivityAsync(
        MessageActivityInput.CreateBuilder()
            .WithText("Welcome to **SSO Bot**! Type `help` to see available commands.", TextFormats.Markdown)
            .Build(), ct);
});

webApp.Run();
