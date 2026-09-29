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
- **Conversation Helpers** &mdash; Send, reply, quote, and typing helpers for reactive and proactive messaging
- **Turn State** &mdash; Conversation and user state backed by `IDistributedCache`
- **Targeted Messages** &mdash; Send messages visible only to a selected participant in supported conversations
- **Observability** &mdash; OpenTelemetry spans, metrics, and Agent 365 baggage propagation
- **Fluent Configuration** &mdash; Chainable handler registration and options-based service configuration
- **Socket Mode** &mdash; Receive activities over an outbound WebSocket during development, with no public endpoint or tunnel

## Installation

```shell
dotnet add package Microsoft.Teams.Apps
```

## Quick Start

```csharp
using Microsoft.Teams.Apps;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddTeamsBotApplication();

var app = builder.Build();
var teams = app.UseTeamsBotApplication(); // maps POST /api/messages

teams.OnMessage(async (context, ct) =>
{
    await context.SendAsync($"You said: {context.Activity.Text}", ct);
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
    await context.SendAsync("Here's how to use the bot...", ct);
});
```

### Conversation Helpers

```csharp
teams.OnMessage(async (context, ct) =>
{
    await context.TypingAsync(ct);
    await context.ReplyAsync("This reply quotes the incoming message.", ct);
    await context.QuoteAsync(context.Activity.Id!, "Explicitly quoted reply.", ct);
});
```

### Invoke Activities

```csharp
// Adaptive card actions
teams.OnAdaptiveCardAction(async (context, ct) =>
{
    var value = context.Activity.Value;
    return AdaptiveCardResponse.CreateMessageResponse("Action received.");
});

// Message extension search
teams.OnQuery(async (context, ct) =>
{
    var query = context.Activity.Value;
    var searchText = query?.Parameters
        .FirstOrDefault(parameter => parameter.Name == "queryText")?
        .Value;

    return MessageExtensionResponse.CreateBuilder()
        .WithType(MessageExtensionResponseTypes.Message)
        .WithText($"Searching for: {searchText}")
        .Build();
});

// Task modules
teams.OnFetchTask(async (context, ct) => { ... });
teams.OnTaskSubmit(async (context, ct) => { ... });

// Link unfurling
teams.OnQueryLink(async (context, ct) => { ... });
```

Other message extension handlers include `OnSubmitAction`, `OnSelectItem`,
`OnAnonQueryLink`, `OnQuerySettingUrl`, `OnSetting`, and
`OnCardButtonClicked`.

## State and OAuth

Enable distributed turn state and register OAuth flows when adding the application:

```csharp
builder.Services.AddTeamsBotApplication(options =>
{
    options.UseState();
    options.AddOAuthFlow("graph");
});
```

`UseState()` uses an in-memory `IDistributedCache` by default. Register another
`IDistributedCache` implementation, such as Redis, for state that must survive
process restarts or be shared across instances.

## Socket Mode

Socket Mode receives activities over outbound WebSocket connections that the bot opens to the Teams service,
instead of an HTTP messaging endpoint, so there is no public URL or dev tunnel to expose. Only inbound delivery
changes: handlers and outbound sends work the same way.

WebSocket is only recommended for use when developing agents. Socket Mode bots should not be submitted to
Marketplace for publishing.

Socket Mode runs on a generic host with no web server:

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Teams.Apps;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddTeamsBotApplication(options => options.UseSocketMode());

var host = builder.Build();
var teams = host.UseTeamsBotApplication(socket: true);

teams.OnMessage(async (context, ct) =>
{
    await context.ReplyAsync($"You said: {context.Activity.Text}", ct);
});

host.Run();
```

- **One connection per geo** &mdash; The bot connects to `amer`, `emea`, and `apac` by default. Startup waits until
  every geo is ready and fails if any geo cannot connect within `StartupTimeout`. Dropped connections reconnect
  automatically, and connection tokens are rotated before they expire.
- **No web server** &mdash; `UseTeamsBotApplication(socket: true)` is required when Socket Mode is enabled, and
  throws on a `WebApplication`; a host that includes a web server fails to start. Tabs, OAuth callbacks, health endpoints, and other HTTP routes are unavailable.
- **Public cloud only** &mdash; Registration fails for bots configured for another cloud.
- **Classic bot identity only** &mdash; The connection is negotiated with the bot's app ID and credentials. Agentic
  identities are not supported.
- **Canary endpoint** &mdash; Socket Mode is currently available only on the canary ring, which `NegotiateBaseUrl`
  targets by default.
- **Rejected credentials are not retried** &mdash; If negotiation returns HTTP 401 or 403, startup fails immediately.
  After startup, the affected geo stops reconnecting until the app restarts.
- **Idempotent handlers** &mdash; The service can redeliver an activity, for example after a reconnect, so handlers
  should tolerate running more than once for the same activity.

`UseSocketMode` accepts a `SocketModeOptions` callback to change the geos, negotiate URL, and connection timeouts.

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
| `MessageActivityInput` | Fluent outbound message model for text, cards, mentions, citations, and actions |
| `TurnStateContainer` | Per-turn access to conversation and user state |
