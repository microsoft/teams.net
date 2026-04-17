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

## Construction & Scoping

### The serviceUrl problem

The Bot Framework service URL is per-request (comes from `activity.ServiceUrl`), but `ApiClient` is per-application (DI singleton). The `ApiClient` solves this with a two-step pattern:

1. **DI registration** creates a base `ApiClient` without a serviceUrl
2. **Per-request**, `ForServiceUrl(uri)` creates a lightweight scoped copy with all sub-clients bound

### DI-friendly constructor (no serviceUrl)

```csharp
// Registered automatically by AddTeamsBotApplication()
// The [ActivatorUtilitiesConstructor] attribute tells DI to prefer this constructor
public ApiClient(HttpClient httpClient, ConversationClient conversationClient, ILogger? logger = null, ...)
```

`AddTeamsBotApplication()` calls `AddBotClient<ApiClient>(...)` which registers `ApiClient` as a typed HTTP client with `BotAuthenticationHandler`. The `ConversationClient` dependency is resolved from DI automatically.

**Important:** The base `ApiClient` has `Conversations`, `Teams`, and `Meetings` set to `null`. Accessing them directly causes `NullReferenceException`. Always use `ForServiceUrl()` or `Context.Api` to get a scoped instance.

### Per-request scoping via Context.Api

In activity handlers, use the `Context.Api` property which auto-scopes to the current activity's service URL:

```csharp
// In a handler — Context.Api is lazy-initialized via ForServiceUrl(Activity.ServiceUrl)
botApp.OnMessage(async (ctx, ct) =>
{
    var members = await ctx.Api.Conversations.Members.GetAsync(conversationId, ct);
    var team = await ctx.Api.Teams.GetByIdAsync(teamId, ct);
});
```

**Do NOT use `ctx.TeamsBotApplication.Api.Conversations`** — that is the unscoped base client and will throw `NullReferenceException`.

### ForServiceUrl (explicit scoping)

For code outside handlers (e.g., proactive messaging, compat layer):

```csharp
ApiClient scoped = baseApiClient.ForServiceUrl(activity.ServiceUrl);
await scoped.Conversations.Activities.CreateAsync(conversationId, activity);
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

## Delegation Pattern

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
├── ApiClient.cs              Top-level facade, DI entry point, ForServiceUrl factory
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

## Integration with Context and Handlers

The `Context<TActivity>` class exposes a lazy `Api` property:

```csharp
public ApiClient Api => _api ??= TeamsBotApplication.Api.ForServiceUrl(Activity.ServiceUrl);
```

This is the primary way handlers should access the API clients. It ensures the scoped `ApiClient` is created once per request and reused across multiple calls within the same handler.

## Integration with CompatTeamsInfo

`CompatTeamsInfo` retrieves `ApiClient` from `TurnState` and uses sub-clients for Teams-specific operations:

- `client.Meetings.GetByIdAsync(meetingId)` — meeting info
- `client.Meetings.GetParticipantAsync(meetingId, participantId, tenantId)` — meeting participant
- `client.Teams.GetByIdAsync(teamId)` — team details
- `client.Teams.GetConversationsAsync(teamId)` — channel list

Member operations go through the core `ConversationClient` directly (not via `ApiClient`).

The `CompatAdapter` should scope the `ApiClient` before storing it in `TurnState`:

```csharp
ApiClient scopedClient = _teamsBotApplication.TeamsApiClient.ForServiceUrl(new Uri(activity.ServiceUrl));
turnContext.TurnState.Add<ApiClient>(scopedClient);
```

## Future Work

- **BatchClient**: Batch messaging operations (`SendMessageToListOfUsersAsync`, etc.) need a new sub-client on `ApiClient` using `BotHttpClient` for the `v3/batch/conversation/` endpoints.
- **MeetingClient.SendMeetingNotificationAsync**: Meeting notification support needs to be added along with notification model types.
