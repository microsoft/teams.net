<!-- Copyright (c) Microsoft Corporation. All rights reserved.-->
<!-- Licensed under the MIT License.-->

# Microsoft.Teams.Apps.BotBuilder

A compatibility bridge that enables existing **Bot Framework SDK v4** applications to run on the modern **Microsoft Teams Bot Core** infrastructure. It implements the Adapter pattern, translating between Bot Framework interfaces and the new Teams Core SDK &mdash; allowing migration without rewriting existing bot logic.

## Key Features

- **Drop-in Adapter** &mdash; `TeamsBotFrameworkHttpAdapter` implements `IBotFrameworkHttpAdapter`, so existing bots work with minimal changes
- **Conversation Support** &mdash; Send, update, and delete activities and manage conversation members through adapted interfaces
- **OAuth Compatibility** &mdash; `CompatUserTokenClient` bridges token management between the two frameworks
- **Proactive Messaging** &mdash; `ContinueConversationAsync` for resuming conversations from external triggers
- **Teams API Access** &mdash; Static `TeamsApiClient` methods for Teams-specific operations (meetings, batch messaging, team/channel metadata)
- **Schema Translation** &mdash; Bidirectional conversion between Bot Framework and Core activity models

## Installation

```shell
dotnet add package Microsoft.Teams.Apps.BotBuilder
```

## Quick Start

### Register the Adapter

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddTeamsBotFrameworkHttpAdapter();
builder.Services.AddTransient<IBot, MyBot>();

var app = builder.Build();

app.MapPost("/api/messages", async (
    IBotFrameworkHttpAdapter adapter,
    IBot bot,
    HttpRequest request,
    HttpResponse response,
    CancellationToken ct) =>
        await adapter.ProcessAsync(request, response, bot, ct))
    .RequireAuthorization();

app.Run();
```

### Use with an Existing Bot

Your existing `IBot` implementation works unchanged:

```csharp
public class MyBot : ActivityHandler
{
    protected override async Task OnMessageActivityAsync(
        ITurnContext<IMessageActivity> turnContext, CancellationToken ct)
    {
        await turnContext.SendActivityAsync(
            MessageFactory.Text($"Echo: {turnContext.Activity.Text}"), ct);
    }
}
```

The compatibility connector implements conversation operations. The Bot
Framework `IConnectorClient.Attachments` API is not implemented; send
attachments as part of activities instead.

### Teams-Specific Operations

Use the static `TeamsApiClient` for Teams APIs within your bot handlers:

```csharp
// Get a specific member
var member = await TeamsApiClient.GetMemberAsync(turnContext, userId);

// Get team details
var team = await TeamsApiClient.GetTeamDetailsAsync(turnContext);

// Get paginated team members
var members = await TeamsApiClient.GetPagedTeamMembersAsync(turnContext, teamId);

// Meeting info
var meeting = await TeamsApiClient.GetMeetingInfoAsync(turnContext);

// Send notification to a meeting
await TeamsApiClient.SendMeetingNotificationAsync(turnContext, notification);
```

### Batch Messaging

```csharp
// Send to all users in a team
var operationId = await TeamsApiClient.SendMessageToAllUsersInTeamAsync(
    turnContext, activity, teamId, tenantId);

// Send to all users in tenant
var operationId = await TeamsApiClient.SendMessageToAllUsersInTenantAsync(
    turnContext, activity, tenantId);

// Check operation status
var state = await TeamsApiClient.GetOperationStateAsync(turnContext, operationId);

// Get failed entries
var failures = await TeamsApiClient.GetPagedFailedEntriesAsync(turnContext, operationId);
```

### Proactive Messaging

```csharp
var adapter = (TeamsBotFrameworkHttpAdapter)serviceProvider
    .GetRequiredService<IBotFrameworkHttpAdapter>();

await adapter.ContinueConversationAsync(
    botId, conversationReference,
    async (turnContext, ct) =>
    {
        await turnContext.SendActivityAsync("Proactive notification!", cancellationToken: ct);
    });
```

## Architecture

The library bridges two frameworks through a set of adapter classes:

```
Bot Framework SDK                    Teams Bot Core
─────────────────                    ──────────────
IBotFrameworkHttpAdapter  ←──→  TeamsBotFrameworkHttpAdapter
BotAdapter                ←──→  TeamsBotAdapter
IConnectorClient          ←──→  CompatConnectorClient
IConversations            ←──→  CompatConversations  →  ConversationClient
UserTokenClient           ←──→  CompatUserTokenClient → Core UserTokenClient
Activity (BF)             ←──→  CoreActivity          (ActivitySchemaMapper)
```

- **`TeamsBotFrameworkHttpAdapter`** handles HTTP request/response lifecycle and delegates to the Core SDK
- **`CompatConversations`** implements `IConversations` by forwarding calls to the Core `ConversationClient`
- **`CompatUserTokenClient`** adapts Core token operations to the Bot Framework `UserTokenClient` interface
- **`ActivitySchemaMapper`** provides bidirectional conversion between Bot Framework `Activity` and Core `CoreActivity`
- **`TeamsApiClient`** provides static methods for Teams-specific APIs not covered by standard Bot Framework interfaces

## Main Types

| Type | Description |
|------|-------------|
| `TeamsBotFrameworkHttpAdapter` | Primary adapter &mdash; implements `IBotFrameworkHttpAdapter` with full HTTP lifecycle |
| `TeamsBotAdapter` | Base adapter bridging `BotAdapter` to Teams Core activity processing |
| `TeamsApiClient` | Static utility for Teams-specific APIs (members, meetings, batch messaging, channels) |
| `ActivitySchemaMapper` | Bidirectional conversion between Bot Framework and Core activity schemas |
| `CompatHostingExtensions` | DI registration via `AddTeamsBotFrameworkHttpAdapter()` |
