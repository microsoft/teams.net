// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// This sample demonstrates Teams SSO using the context-level API with a single OAuth connection.
// The context API is the simplest way to add authentication -- when only one OAuthFlow is registered,
// context.SignIn() and context.SignOut() automatically resolve to it without specifying a connection name.
//
// Azure Bot resource must have one OAuth connection setting configured:
// | Connection name   | Provider    | Scopes                   |
// |-------------------|-------------|--------------------------|
// | GraphConnection   | Azure AD v2 | User.Read Calendars.Read |

using Microsoft.Teams.Bot.Apps;
using Microsoft.Teams.Bot.Apps.Auth;
using Microsoft.Teams.Bot.Apps.Handlers;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Core.Schema;

WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);
webAppBuilder.Services.AddTeamsBotApplication();
WebApplication webApp = webAppBuilder.Build();

TeamsBotApplication bot = webApp.UseTeamsBotApplication();

// Register a single OAuthFlow -- this becomes the default for all context.SignIn/SignOut calls
OAuthFlow auth = bot.AddOAuthFlow("sso");

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
    // context.SignIn() resolves to the single registered OAuthFlow automatically
    string? token = await context.SignIn(cancellationToken: ct);
    if (token is not null)
    {
        await context.SendActivityAsync("You're already signed in.", ct);
    }
    // else: OAuthCard sent, SSO flow in progress -- OnSignInComplete will fire
});

bot.OnMessage("(?i)^profile$", async (context, ct) =>
{
    // SignIn doubles as "get token if cached, else start sign-in"
    string? token = await context.SignIn(cancellationToken: ct);
    if (token is null) return; // sign-in card sent, wait for completion

    using var http = new HttpClient();
    http.DefaultRequestHeaders.Authorization = new("Bearer", token);

    try
    {
        string json = await http.GetStringAsync("https://graph.microsoft.com/v1.0/me", ct);
        await context.SendActivityAsync($"```json\n{json}\n```", ct);
    }
    catch (HttpRequestException ex)
    {
        await context.SendActivityAsync($"Graph call failed: {ex.Message}", ct);
    }
});

bot.OnMessage("(?i)^calendar$", async (context, ct) =>
{
    string? token = await context.SignIn(cancellationToken: ct);
    if (token is null) return;

    using var http = new HttpClient();
    http.DefaultRequestHeaders.Authorization = new("Bearer", token);

    try
    {
        string json = await http.GetStringAsync(
            "https://graph.microsoft.com/v1.0/me/events?$top=3&$select=subject,start,end&$orderby=start/dateTime", ct);
        await context.SendActivityAsync($"```json\n{json}\n```", ct);
    }
    catch (HttpRequestException ex)
    {
        await context.SendActivityAsync($"Graph call failed: {ex.Message}", ct);
    }
});

bot.OnMessage("(?i)^logout$", async (context, ct) =>
{
    await context.SignOut(cancellationToken: ct);
    await context.SendActivityAsync("Signed out.", ct);
});

bot.OnMessage("(?i)^status$", async (context, ct) =>
{
    bool signedIn = await context.IsSignedInAsync(cancellationToken: ct);
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
        new MessageActivity(helpText) { TextFormat = TextFormats.Markdown }, ct);
});

// ==================== INSTALL HANDLER ====================

bot.OnInstall(async (context, ct) =>
{
    await context.SendActivityAsync(
        new MessageActivity("Welcome to **SSO Bot**! Type `help` to see available commands.")
        {
            TextFormat = TextFormats.Markdown
        }, ct);
});

webApp.Run();
