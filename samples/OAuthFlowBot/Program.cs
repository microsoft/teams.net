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
using Microsoft.Teams.Apps.OAuth;
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

// Configure distributed cache for turn state persistence. This is optional, but recommended for production scenarios.
// The sample below uses Redis, but you can use any IDistributedCache implementation.
/*
webAppBuilder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = webAppBuilder.Configuration.GetConnectionString("Redis") ?? throw new InvalidProgramException("Redis connection string not found");
});*/

WebApplication webApp = webAppBuilder.Build();

TeamsBotApplication bot = webApp.UseTeamsBotApplication();

// ==================== OAUTH FLOW SETUP ====================

// Get the pre-registered flows and attach callbacks
OAuthFlow graphAuth = bot.GetOAuthFlow("sso");
OAuthFlow githubAuth = bot.GetOAuthFlow("gh");

graphAuth.OnSignInComplete(async (context, tokenResponse, ct) =>
{
    await context.SendAsync($"User {context.Activity.From?.Name} connected to Microsoft Graph ({tokenResponse.ConnectionName})!", ct);
});

graphAuth.OnSignInFailure(async (context, failure, ct) =>
{
    await context.SendAsync($"User {context.Activity.From?.Name} failed to connect to Microsoft Graph. {failure?.Message}", ct);
});

githubAuth.OnSignInComplete(async (context, tokenResponse, ct) =>
{
    await context.SendAsync($"User {context.Activity.From?.Name} connected to GitHub ({tokenResponse.ConnectionName})!", ct);
});

githubAuth.OnSignInFailure(async (context, failure, ct) =>
{
    await context.SendAsync($"User {context.Activity.From?.Name} failed to connect to GitHub. {failure?.Message}", ct);
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

    await context.SendAsync(
        new MessageActivityInput().WithText(helpText, TextFormats.Markdown), ct);
});

bot.OnMessage("(?i)^login$", async (context, ct) =>
{
    string? tokenGitHub = await githubAuth.SignInAsync(context, ct);
    string? tokenGraph = await graphAuth.SignInAsync(context, ct);
    if (tokenGraph is not null)
    {
        await context.SendAsync("Already signed in to Graph.", ct);
    }

    if (tokenGitHub is not null)
    {
        await context.SendAsync("Already signed in to GitHub.", ct);
    }

});

bot.OnMessage("(?i)^login graph$", async (context, ct) =>
{
    string? tokenGraph = await graphAuth.SignInAsync(context, ct);
    if (tokenGraph is not null)
    {
        await context.SendAsync("Already signed in to Graph.", ct);
    }
    // else: OAuthCard sent, SSO in progress
});

bot.OnMessage("(?i)^login github$", async (context, ct) =>
{
    string? tokenGitHub = await githubAuth.SignInAsync(context, ct);
    if (tokenGitHub is not null)
    {
        await context.SendAsync("Already signed in to GitHub.", ct);
    }
});

bot.OnMessage("(?i)^status$", async (context, ct) =>
{
    // GetConnectionStatusAsync returns ALL connections -- no names needed
    IList<GetTokenStatusResult> statuses = await context.GetConnectionStatusAsync(ct);
    IEnumerable<string> lines = statuses.Select(s =>
        $"- **{s.ConnectionName}** ({s.ServiceProviderDisplayName}): " +
        $"{(s.HasToken == true ? "✅ connected" : "❌ not connected")}");

    await context.SendAsync(
        new MessageActivityInput()
            .WithText($"OAuth connections for {context.Activity.From?.Name} :\n" + string.Join("\n", lines), TextFormats.Markdown)
            , ct);
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
        await context.SendAsync($"Your Azure AD user :\n```json\n{response}\n```", ct);
    }
    catch (HttpRequestException ex)
    {
        await context.SendAsync($"Failed to fetch Azure AD user: {ex.Message}", ct);
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
        await context.SendAsync($"Your GitHub user :\n```json\n{response}\n```", ct);
    }
    catch (HttpRequestException ex)
    {
        await context.SendAsync($"Failed to fetch GitHub user: {ex.Message}", ct);
    }
});

bot.OnMessage("(?i)^logout$", async (context, ct) =>
{
    await graphAuth.SignOutAsync(context, ct);
    await githubAuth.SignOutAsync(context, ct);
    await context.SendAsync("Signed out from all services.", ct);
});

bot.OnMessage("(?i)^logout graph$", async (context, ct) =>
{
    await graphAuth.SignOutAsync(context, ct);
    await context.SendAsync("Signed out from Graph.", ct);
});

bot.OnMessage("(?i)^logout github$", async (context, ct) =>
{
    await githubAuth.SignOutAsync(context, ct);
    await context.SendAsync("Signed out from GitHub.", ct);
});

// ==================== INSTALL HANDLER ====================

bot.OnInstall(async (context, ct) =>
{
    await context.SendAsync(
        new MessageActivityInput()
            .WithText("Welcome to the **OAuthFlow Bot**! Type `help` to see available commands.", TextFormats.Markdown)
            , ct);
});

webApp.Run();
