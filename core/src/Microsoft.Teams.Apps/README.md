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
using Microsoft.Teams.Apps.Handlers;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddTeamsBotApplication();

var app = builder.Build();
var teams = app.UseTeamsBotApplication(); // maps POST /api/messages

teams.OnMessage(async (context, ct) =>
{
    await context.SendActivityAsync($"You said: {context.Activity.Text}", ct);
});


app.Run();
```

## Handler Registration

Handlers are registered as extension methods on `TeamsBotApplication` and can be chained:

### Messages

```csharp
using Microsoft.Teams.Apps.Handlers;
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
