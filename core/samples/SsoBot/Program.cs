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

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.OAuth;
using Microsoft.Teams.Apps.Schema;

WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);

var appBuilder = App.Builder().AddOAuth("sso");

webAppBuilder.AddTeams(appBuilder);

// Configure the single OAuth flow at the DI level
//webAppBuilder.Services.AddTeamsBotApplication(options =>
//{
//    options.AddOAuthFlow("sso");
//});

WebApplication webApp = webAppBuilder.Build();

TeamsBotApplication bot = webApp.UseTeamsBotApplication();

// Get the pre-registered flow and attach callbacks
OAuthFlow auth = bot.GetOAuthFlow("sso");

auth.OnSignInComplete(async (context, tokenResponse, ct) =>
{
    await context.SendActivityAsync(new MessageActivity("You're now signed in! Try `profile` or `calendar`.")
        .WithSuggestedActions(
            new SuggestedActions()
            {
                Actions = new List<SuggestedAction>()
                {
                  new SuggestedAction() { Title = "Profile", Type = "imBack", Value = "profile" },
                  new SuggestedAction() { Title = "Calendar", Type = "imBack", Value = "calendar" }
            }
            }), ct);
});

auth.OnSignInFailure(async (context, failure, ct) =>
{
    string message = failure is not null
        ? $"User {context.Activity.From?.Name} Sign-in failed: {failure.Code} — {failure.Message}"
        : "Sign-in failed. Please try again.";
    var signInFailureMessage = new MessageActivity(message)
    {
        TextFormat = TextFormats.Markdown
    };
    signInFailureMessage.Recipient = context.Activity.From;
    signInFailureMessage.Recipient?.IsTargeted = context.Activity?.Conversation?.ConversationType == ConversationType.GroupChat; // only set IsTargeted for 1:1 chats to avoid issues in group contexts
    await context.SendActivityAsync(signInFailureMessage, ct);
});

// ==================== MESSAGE HANDLERS ====================

bot.OnMessage("(?i)^login$", async (context, ct) =>
{
    // context.SignIn() resolves to the single registered OAuthFlow automatically
    string? token = await context.SignIn(cancellationToken: ct);
    if (token is not null)
    {
        var alreadySignedInMessage = new MessageActivity($"You're already signed in, {context.Activity.From?.Name}!")
        {
            TextFormat = TextFormats.Markdown
        };
        alreadySignedInMessage.Recipient = context.Activity.From;
        alreadySignedInMessage.Recipient?.IsTargeted = context.Activity?.Conversation?.ConversationType == ConversationType.GroupChat; // only set IsTargeted for 1:1 chats to avoid issues in group contexts
        await context.SendActivityAsync(alreadySignedInMessage, ct);
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
        string indentedJson = JsonSerializer.Serialize(JsonSerializer.Deserialize<JsonObject>(json), new JsonSerializerOptions { WriteIndented = true });

        var msgResponse = new MessageActivity($" ## Graph Me [{context.Activity.From?.Name}] \n ```json\n{indentedJson}\n```")
        { TextFormat = TextFormats.Markdown };
        msgResponse.Recipient = context.Activity.From;
        msgResponse.Recipient?.IsTargeted = context.Activity?.Conversation?.ConversationType == ConversationType.GroupChat; // only set IsTargeted for 1:1 chats to avoid issues in group contexts
        await context.SendActivityAsync(msgResponse, ct);
    }
    catch (HttpRequestException ex)
    {
        await context.SendActivityAsync($"[{context.Activity.From?.Name}] Graph call failed: {ex.Message}", ct);
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
        string indentedJson = JsonSerializer.Serialize(JsonSerializer.Deserialize<JsonObject>(json), new JsonSerializerOptions { WriteIndented = true });
        await context.SendActivityAsync(new MessageActivity($" ## Graph Calendar [{context.Activity.From?.Name}] \n ```json\n{indentedJson}\n```") { TextFormat = TextFormats.Markdown }, ct);
    }
    catch (HttpRequestException ex)
    {
        await context.SendActivityAsync($"[{context.Activity.From?.Name}] Graph call failed: {ex.Message}", ct);
    }
});

bot.OnMessage("(?i)^logout$", async (context, ct) =>
{
    await context.SignOut(cancellationToken: ct);
    var signOutMessage = new MessageActivity($"You've been signed out, {context.Activity.From?.Name}.")
    {
        TextFormat = TextFormats.Markdown
    };
    signOutMessage.Recipient = context.Activity.From;
    signOutMessage.Recipient?.IsTargeted = context.Activity?.Conversation?.ConversationType == ConversationType.GroupChat; // only set IsTargeted for 1:1 chats to avoid issues in group contexts
    await context.SendActivityAsync(signOutMessage, ct);
});

bot.OnMessage("(?i)^status$", async (context, ct) =>
{
    bool signedIn = await context.IsSignedInAsync(cancellationToken: ct);
    var signInStatusMessage = new MessageActivity(signedIn
        ? $"User {context.Activity.From?.Name} is signed in."
        : $"User {context.Activity.From?.Name} is not signed in.")
    {
        TextFormat = TextFormats.Markdown
    };
    signInStatusMessage.Recipient = context.Activity.From;
    signInStatusMessage.Recipient?.IsTargeted = context.Activity?.Conversation?.ConversationType == ConversationType.GroupChat; // only set IsTargeted for 1:1 chats to avoid issues in group contexts
    await context.SendActivityAsync(signInStatusMessage, ct);
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
        new MessageActivity(helpText) { TextFormat = TextFormats.Markdown }
        .WithSuggestedActions(
            new SuggestedActions() {
                Actions = new List<SuggestedAction>()
                {
                  new SuggestedAction() { Title = "Login", Type = "imBack", Value = "login" },
                  new SuggestedAction() { Title = "Logout", Type = "imBack", Value = "logout" },
                  new SuggestedAction() { Title = "Status", Type = "imBack", Value = "status" },
            }
            }), ct);
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
