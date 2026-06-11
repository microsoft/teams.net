// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// This sample demonstrates how to use OAuthFlow with two OAuth connections:
// - GraphConnection: Microsoft Graph (Azure AD v2) for user profile and calendar
// - GitHubConnection: GitHub for repositories
//
// Azure Bot resource must have two OAuth connection settings configured:
// | Connection name   | Provider     | Scopes                    |
// |-------------------|--------------|---------------------------|
// | GraphConnection   | Azure AD v2  | User.Read Calendars.Read  |
// | GitHubConnection  | GitHub       | repo read:user            |

using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.OAuth;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core;

WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);

// Configure OAuth flows at the DI level -- card text is set once here
webAppBuilder.Services.AddTeamsBotApplication(options =>
{
    options.AddOAuthFlow("sso", o =>
    {
        o.OAuthCardText = "Sign in to your Microsoft account";
        o.SignInButtonText = "Sign In to Graph";
    });
    options.AddOAuthFlow("gh", o =>
    {
        o.OAuthCardText = "Sign in to your GitHub account";
        o.SignInButtonText = "Sign In to GitHub";
    });
});

WebApplication webApp = webAppBuilder.Build();

TeamsBotApplication bot = webApp.UseTeamsBotApplication();

// ==================== OAUTH FLOW SETUP ====================

// Get the pre-registered flows and attach callbacks
OAuthFlow graphAuth = bot.GetOAuthFlow("sso");
OAuthFlow githubAuth = bot.GetOAuthFlow("gh");

graphAuth.OnSignInComplete(async (context, tokenResponse, ct) =>
{
    await context.SendActivityAsync($"User {context.Activity.From?.Name} connected to Microsoft Graph ({tokenResponse.ConnectionName})!", ct);
});

graphAuth.OnSignInFailure(async (context, failure, ct) =>
{
    await context.SendActivityAsync($"User {context.Activity.From?.Name} failed to connect to Microsoft Graph. {failure?.Message}", ct);
});

githubAuth.OnSignInComplete(async (context, tokenResponse, ct) =>
{
    await context.SendActivityAsync($"User {context.Activity.From?.Name} connected to GitHub ({tokenResponse.ConnectionName})!", ct);
});

githubAuth.OnSignInFailure(async (context, failure, ct) =>
{
    await context.SendActivityAsync($"User {context.Activity.From?.Name} failed to connect to GitHub. {failure?.Message}", ct);
});

// ==================== MESSAGE HANDLERS ====================

bot.OnMessage("(?i)^help$", async (context, ct) =>
{
    string helpText = """
        **OAuthFlow Bot** - Multi-connection OAuth sample

        Commands:
        - `login` - Sign in to all connections
        - `login graph` - Sign in to Microsoft Graph
        - `login github` - Sign in to GitHub
        - `status` - Show OAuth connection status
        - `my ad user` - Get your Azure AD user (requires Graph)
        - `my gh user` - Get your GitHub user (requires GitHub)
        - `logout` - Sign out from all connections
        - `logout graph` - Sign out from Graph only
        - `logout github` - Sign out from GitHub only
        - `help` - Show this message
        """;

    await context.SendActivityAsync(
        new MessageActivity(helpText) { TextFormat = TextFormats.Markdown }, ct);
});

bot.OnMessage("(?i)^login$", async (context, ct) =>
{
    // Sign in to Graph first; if an OAuthCard was sent (null return), don't
    // send a second card for GitHub — the user should complete one at a time.
    string? tokenGraph = await graphAuth.SignInAsync(context, ct);
    if (tokenGraph is null) return; // OAuthCard sent, wait for completion

    string? tokenGitHub = await githubAuth.SignInAsync(context, ct);
    if (tokenGitHub is null) return;

    await context.SendActivityAsync("Already signed in to both Graph and GitHub.", ct);
});

bot.OnMessage("(?i)^login graph$", async (context, ct) =>
{
    string? tokenGraph = await graphAuth.SignInAsync(context, ct);
    if (tokenGraph is not null)
    {
        await context.SendActivityAsync("Already signed in to Graph.", ct);
    }
    // else: OAuthCard sent, SSO in progress
});

bot.OnMessage("(?i)^login github$", async (context, ct) =>
{
    string? tokenGitHub = await githubAuth.SignInAsync(context, ct);
    if (tokenGitHub is not null)
    {
        await context.SendActivityAsync("Already signed in to GitHub.", ct);
    }
});

bot.OnMessage("(?i)^status$", async (context, ct) =>
{
    // GetConnectionStatusAsync returns ALL connections -- no names needed
    IList<GetTokenStatusResult> statuses = await graphAuth.GetConnectionStatusAsync(context, ct);
    IEnumerable<string> lines = statuses.Select(s =>
        $"- **{s.ConnectionName}** ({s.ServiceProviderDisplayName}): " +
        $"{(s.HasToken == true ? "✅ connected" : "❌ not connected")}");

    await context.SendActivityAsync(
        new MessageActivity($"OAuth connections for {context.Activity.From?.Name} :\n" + string.Join("\n", lines))
        {
            TextFormat = TextFormats.Markdown
        }, ct);
});

bot.OnMessage("(?i)^my ad user", async (context, ct) =>
{
    string? token = await graphAuth.SignInAsync(context, ct);
    if (token is null) return;

    using HttpClient http = new();
    http.DefaultRequestHeaders.Authorization = new("Bearer", token);

    try
    {
        string response = await http.GetStringAsync(
            "https://graph.microsoft.com/v1.0/me", ct);
        await context.SendActivityAsync($"Your Azure AD user :\n```json\n{response}\n```", ct);
    }
    catch (HttpRequestException ex)
    {
        await context.SendActivityAsync($"Failed to fetch Azure AD user: {ex.Message}", ct);
    }
});

bot.OnMessage("(?i)^my gh user$", async (context, ct) =>
{
    string? token = await githubAuth.SignInAsync(context, ct);
    if (token is null) return;

    using HttpClient http = new();
    http.DefaultRequestHeaders.Authorization = new("Bearer", token);
    http.DefaultRequestHeaders.UserAgent.ParseAdd("TeamsBot/1.0");

    try
    {
        string response = await http.GetStringAsync(
            "https://api.github.com/user", ct);
        await context.SendActivityAsync($"Your GitHub user :\n```json\n{response}\n```", ct);
    }
    catch (HttpRequestException ex)
    {
        await context.SendActivityAsync($"Failed to fetch GitHub user: {ex.Message}", ct);
    }
});

bot.OnMessage("(?i)^logout$", async (context, ct) =>
{
    await graphAuth.SignOutAsync(context, ct);
    await githubAuth.SignOutAsync(context, ct);
    await context.SendActivityAsync("Signed out from all services.", ct);
});

bot.OnMessage("(?i)^logout graph$", async (context, ct) =>
{
    await graphAuth.SignOutAsync(context, ct);
    await context.SendActivityAsync("Signed out from Graph.", ct);
});

bot.OnMessage("(?i)^logout github$", async (context, ct) =>
{
    await githubAuth.SignOutAsync(context, ct);
    await context.SendActivityAsync("Signed out from GitHub.", ct);
});

// ==================== INSTALL HANDLER ====================

bot.OnInstall(async (context, ct) =>
{
    await context.SendActivityAsync(
        new MessageActivity("Welcome to the **OAuthFlow Bot**! Type `help` to see available commands.")
        {
            TextFormat = TextFormats.Markdown
        }, ct);
});

webApp.Run();
