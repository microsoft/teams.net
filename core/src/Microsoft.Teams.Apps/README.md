<!-- Copyright (c) Microsoft Corporation. All rights reserved.-->
<!-- Licensed under the MIT License.-->

# Microsoft.Teams.Apps

A high-level framework for building Microsoft Teams bots in .NET. Built on top of `Microsoft.Teams.Core`, it provides Teams-specific activity types, a typed routing and handler system, OAuth authentication flows, Teams API clients, and streaming message support.

## Key Features

- **Typed Activity Routing** &mdash; Register handlers for specific activity types (`OnMessage`, `OnAdaptiveCardAction`, `OnQuery`, etc.) with type-safe contexts
- **Teams Activity Schema** &mdash; Rich type hierarchy (`MessageActivity`, `InvokeActivity<T>`, `ConversationUpdateActivity`, etc.) with polymorphic deserialization
- **OAuth Flows** &mdash; Built-in SSO token exchange, sign-in cards, and token management via `OAuthFlow`
- **Teams API Clients** &mdash; Typed clients for conversations, members, teams, channels, meetings, and batch operations
- **Streaming Messages** &mdash; Progressive response updates via `TeamsStreamingWriter`
- **Fluent Configuration** &mdash; Chainable handler registration and `AppBuilder` for setup

## Installation

```shell
dotnet add package Microsoft.Teams.Apps
```

## Quick Start

```csharp
using Microsoft.Teams.Apps;

var builder = WebApplication.CreateBuilder(args);
builder.AddTeams();

var app = builder.Build();
var teams = app.UseTeams(); // maps POST /api/messages

teams.OnMessage(async (context, ct) =>
{
    await context.Send($"You said: {context.Activity.Text}");
});

teams.OnMembersAdded(async (context, ct) =>
{
    foreach (var member in context.Activity.MembersAdded)
        await context.Send($"Welcome, {member.Name}!");
});

app.Run();
```

## Handler Registration

Handlers are registered as extension methods on `TeamsBotApplication` and can be chained:

### Messages

```csharp
// All messages
teams.OnMessage(async (context, ct) => { ... });

// Regex pattern match
teams.OnMessage(@"^help$", async (context, ct) =>
{
    await context.Send("Here's how to use the bot...");
});
```

### Invoke Activities

```csharp
// Adaptive card actions
teams.OnAdaptiveCardAction(async (context, ct) =>
{
    var value = context.Activity.Value;
    return new InvokeResponse(200);
});

// Message extension search
teams.OnQuery(async (context, ct) =>
{
    var query = context.Activity.Value.Parameters["queryText"];
    return new InvokeResponse<MessageExtensionResponse>(200, response);
});

// Task modules
teams.OnFetchTask(async (context, ct) => { ... });
teams.OnTaskSubmit(async (context, ct) => { ... });

// Link unfurling
teams.OnQueryLink(async (context, ct) => { ... });
```

### Conversation Updates

```csharp
teams.OnMembersAdded(async (context, ct) => { ... });
teams.OnMembersRemoved(async (context, ct) => { ... });
teams.OnChannelCreated(async (context, ct) => { ... });
teams.OnTeamMemberAdded(async (context, ct) => { ... });
```

### Other Events

```csharp
teams.OnMessageReaction(async (context, ct) => { ... });
teams.OnMessageUpdate(async (context, ct) => { ... });
teams.OnMessageDelete(async (context, ct) => { ... });
teams.OnInstallUpdate(async (context, ct) => { ... });
teams.OnMeeting(async (context, ct) => { ... });
```

## OAuth Authentication

```csharp
// Configure OAuth during setup
var appBuilder = App.Builder().AddOAuth("graph");
builder.AddTeams(appBuilder);

// Register sign-in handlers
var flow = teams.GetOAuthFlow("graph");

flow.OnSignInComplete(async (context, tokenResponse, ct) =>
{
    // Use token to call Microsoft Graph or other APIs
});

flow.OnSignInFailure(async (context, failure, ct) =>
{
    await context.Send("Sign-in failed. Please try again.");
});

// Trigger sign-in from a message handler
teams.OnMessage(async (context, ct) =>
{
    var flow = context.App.GetOAuthFlow("graph");
    await flow.SignInAsync(context, ct);
});
```

## Teams API Clients

Access Teams APIs through the typed `Context.Api` property:

```csharp
teams.OnMessage(async (context, ct) =>
{
    // Get conversation members
    var members = await context.Api.Conversations.Members
        .GetAsync(context.Activity.Conversation.Id);

    // Get team details
    var team = await context.Api.Teams.GetAsync(teamId);

    // Send to a specific channel
    await context.Api.Conversations.Activities
        .SendAsync(channelId, activity);
});
```

## Streaming Responses

Send progressive message updates while the bot processes a request:

```csharp
teams.OnMessage(async (context, ct) =>
{
    var writer = TeamsStreamingWriter.CreateFromContext(context);
    await writer.SendInformativeUpdateAsync("Thinking...");
    await writer.AppendResponseAsync("Here is ");
    await writer.AppendResponseAsync("the answer.");
    await writer.FinalizeResponseAsync();
});
```

## Routing Behavior

- **Non-invoke activities**: All matching routes execute sequentially
- **Invoke activities**: Only the first matching route executes and must return a response
- Route names must be unique; mixing catch-all invoke handlers with specific invoke handlers is not allowed

## Main Types

| Type | Description |
|------|-------------|
| `TeamsBotApplication` | Main entry point &mdash; extends `BotApplication` with Teams-specific routing and features |
| `Context<TActivity>` | Per-turn context providing typed activity access, API clients, and helper methods |
| `TeamsActivity` | Base Teams activity with polymorphic deserialization into specific subtypes |
| `MessageActivity` | Text and attachment messages |
| `InvokeActivity<T>` | Invoke operations (adaptive cards, task modules, message extensions) |
| `ConversationUpdateActivity` | Membership, channel, and team lifecycle events |
| `OAuthFlow` | OAuth sign-in, token exchange (SSO), and sign-out management |
| `ApiClient` | Facade for Teams conversation, member, team, meeting, and bot APIs |
| `TeamsStreamingWriter` | Progressive message streaming with rate limiting |
| `Router` | Internal activity dispatcher matching routes by type and selector |
