# ApiClient Design Document

## Overview

The `ApiClient` class (`Microsoft.Teams.Bot.Apps.Api.Clients`) provides a hierarchical, Libraries-compatible API surface for Teams Bot operations. It organizes Bot Framework v3 REST API calls into sub-clients that delegate to the core SDK infrastructure rather than making raw HTTP calls.

## Architecture

```
ApiClient (top-level facade)
├── Bots              → BotClient
│   └── SignIn        → BotSignInClient           [BotHttpClient → token.botframework.com]
├── Conversations     → V3ConversationClient       [delegates to core ConversationClient]
│   ├── Activities    → ActivityClient
│   ├── Members       → MemberClient
│   └── Reactions     → ReactionClient
├── Users             → UserClient
│   └── Token         → V3UserTokenClient          [BotHttpClient → token.botframework.com]
├── Teams             → TeamClient                 [BotHttpClient → serviceUrl/v3/teams/]
└── Meetings          → MeetingClient              [BotHttpClient → serviceUrl/v1/meetings/]
```

### Two HTTP strategies

| Sub-client | HTTP strategy | Why |
|---|---|---|
| Conversations (Activities, Members, Reactions) | Delegates to core `ConversationClient` | Reuses auth, logging, agents-channel handling, agentic identity support |
| Teams, Meetings | Uses `BotHttpClient` directly | No core client equivalent exists for these endpoints |
| Bots.SignIn, Users.Token | Uses `BotHttpClient` directly | Calls `token.botframework.com`, separate from conversation endpoints |

## Construction Patterns

### DI-friendly (no serviceUrl)

```csharp
// Startup — registered via AddTeamsBotApplication() or manually
services.AddSingleton<ApiClient>(sp =>
{
    HttpClient httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("ApiClient");
    ConversationClient conversationClient = sp.GetRequiredService<ConversationClient>();
    return new ApiClient(httpClient, conversationClient, sp.GetRequiredService<ILogger<ApiClient>>());
});
```

The `[ActivatorUtilitiesConstructor]` attribute marks this as the preferred constructor for DI, avoiding ambiguity with the fully-initialized constructor.

ServiceUrl-dependent sub-clients (`Conversations`, `Teams`, `Meetings`) are `null` until `ForServiceUrl` is called.

### Per-request scoping

```csharp
// Per-request — creates a lightweight copy with serviceUrl-bound sub-clients
ApiClient scoped = baseApiClient.ForServiceUrl(activity.ServiceUrl);

// Now safe to use
await scoped.Conversations.Activities.CreateAsync(conversationId, activity);
await scoped.Teams.GetByIdAsync(teamId);
await scoped.Meetings.GetByIdAsync(meetingId);
```

`ForServiceUrl` shares the underlying `BotHttpClient` and `ConversationClient` — only the sub-client wrappers are new allocations.

### Fully initialized (for tests or known serviceUrl)

```csharp
ApiClient client = new(
    new Uri("https://smba.trafficmanager.net/teams/"),
    httpClient,
    conversationClient,
    logger);
```

## Delegation Pattern (Option C)

The conversation sub-clients (`ActivityClient`, `MemberClient`, `ReactionClient`) delegate to the core `ConversationClient` rather than duplicating HTTP logic. This ensures:

- Single source of truth for URL construction, auth, and error handling
- Agents-channel ID truncation logic is preserved
- Agentic identity support works transparently
- Custom headers and logging from `ConversationClient` apply

### Parameter bridging

The Libraries-style API takes `(conversationId, activity)` as separate parameters, while the core `ConversationClient` expects context embedded in the activity or passed as method parameters. The sub-clients bridge this:

```
ActivityClient.CreateAsync(conversationId, activity)
    → sets activity.ServiceUrl, activity.Conversation
    → calls ConversationClient.SendActivityAsync(activity)

MemberClient.GetAsync(conversationId)
    → calls ConversationClient.GetConversationMembersAsync(conversationId, serviceUrl)

ReactionClient.AddAsync(conversationId, activityId, reactionType)
    → calls ConversationClient.AddReactionAsync(conversationId, activityId, reactionType, serviceUrl)
```

### Method mapping

#### ActivityClient → ConversationClient

| ActivityClient | ConversationClient | Notes |
|---|---|---|
| `CreateAsync(conversationId, activity)` | `SendActivityAsync(activity)` | Sets `ServiceUrl` and `Conversation` on activity |
| `UpdateAsync(conversationId, id, activity)` | `UpdateActivityAsync(conversationId, id, activity)` | Sets `ServiceUrl` on activity |
| `ReplyAsync(conversationId, id, activity)` | `SendActivityAsync(activity)` | Sets `ReplyToId`, `ServiceUrl`, `Conversation` |
| `DeleteAsync(conversationId, id)` | `DeleteActivityAsync(conversationId, id, serviceUrl)` | |
| `CreateTargetedAsync(conversationId, activity)` | `SendActivityAsync(activity)` | Sets `Recipient.IsTargeted = true` |
| `UpdateTargetedAsync(conversationId, id, activity)` | `UpdateTargetedActivityAsync(conversationId, id, activity)` | Sets `ServiceUrl` on activity |
| `DeleteTargetedAsync(conversationId, id)` | `DeleteTargetedActivityAsync(conversationId, id, serviceUrl)` | |

#### MemberClient → ConversationClient

| MemberClient | ConversationClient |
|---|---|
| `GetAsync(conversationId)` | `GetConversationMembersAsync(conversationId, serviceUrl)` |
| `GetByIdAsync(conversationId, memberId)` | `GetConversationMemberAsync<ConversationAccount>(conversationId, memberId, serviceUrl)` |
| `GetByIdAsync<T>(conversationId, memberId)` | `GetConversationMemberAsync<T>(conversationId, memberId, serviceUrl)` |
| `DeleteAsync(conversationId, memberId)` | `DeleteConversationMemberAsync(conversationId, memberId, serviceUrl)` |

#### ReactionClient → ConversationClient

| ReactionClient | ConversationClient |
|---|---|
| `AddAsync(conversationId, activityId, reactionType)` | `AddReactionAsync(conversationId, activityId, reactionType, serviceUrl)` |
| `DeleteAsync(conversationId, activityId, reactionType)` | `DeleteReactionAsync(conversationId, activityId, reactionType, serviceUrl)` |

#### V3ConversationClient → ConversationClient

| V3ConversationClient | ConversationClient |
|---|---|
| `CreateAsync(parameters)` | `CreateConversationAsync(parameters, serviceUrl)` |

## File Layout

```
core/src/Microsoft.Teams.Bot.Apps/Api/Clients/
├── ApiClient.cs              Top-level facade, DI entry point
├── V3ConversationClient.cs   Conversation facade → delegates to core ConversationClient
├── ActivityClient.cs         Activity CRUD → delegates to core ConversationClient
├── MemberClient.cs           Member operations → delegates to core ConversationClient
├── ReactionClient.cs         Reaction operations → delegates to core ConversationClient
├── TeamClient.cs             Team info → BotHttpClient (v3/teams/)
├── MeetingClient.cs          Meeting info → BotHttpClient (v1/meetings/) + models
├── BotClient.cs              Bot facade (groups SignIn)
├── BotSignInClient.cs        Sign-in URLs → BotHttpClient (token.botframework.com)
├── BotTokenClient.cs         Static scope constants
├── UserClient.cs             User facade (groups Token)
└── V3UserTokenClient.cs      User token ops → BotHttpClient (token.botframework.com)
```

## Integration with CompatTeamsInfo

`CompatTeamsInfo` retrieves `ApiClient` from `TurnState` and uses it for Teams-specific operations (meetings, team details, channels). Member operations go through the core `ConversationClient` directly.

The `CompatAdapter` should scope the `ApiClient` per-request before storing it in `TurnState`:

```csharp
ApiClient scopedClient = _teamsBotApplication.TeamsApiClient.ForServiceUrl(new Uri(activity.ServiceUrl));
turnContext.TurnState.Add<ApiClient>(scopedClient);
```

## Future Work

- **BatchClient**: Batch messaging operations (`SendMessageToListOfUsersAsync`, etc.) need a new sub-client on `ApiClient` using `BotHttpClient` for the `v3/batch/conversation/` endpoints.
- **MeetingClient.SendMeetingNotificationAsync**: Meeting notification support needs to be added along with notification model types.
- **DI registration**: `AddTeamsBotApplication` should register `ApiClient` using the DI-friendly constructor automatically.
