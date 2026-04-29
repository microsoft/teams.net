# ApiClient Design Document

## Overview

The `ApiClient` class (`Microsoft.Teams.Apps.Api.Clients`) provides a hierarchical, Libraries-compatible API surface for Teams Bot operations. It organizes Bot Framework v3 REST API calls into sub-clients that delegate to the core SDK infrastructure rather than making raw HTTP calls.

## Architecture

```
ApiClient (top-level facade)
├── Bots              → BotClient
│   └── SignIn        → BotSignInClient              [delegates to core UserTokenClient]
├── Conversations     → ConversationApiClient         [delegates to core ConversationClient]
│   ├── Activities    → ActivityClient
│   ├── Members       → MemberClient
│   └── Reactions     → ReactionClient               [Experimental]
├── Users             → UserClient
│   └── Token         → UserTokenApiClient            [delegates to core UserTokenClient]
├── Teams             → TeamClient                    [BotHttpClient → serviceUrl/v3/teams/]
└── Meetings          → MeetingClient                 [BotHttpClient → serviceUrl/v1/meetings/]
```

### HTTP strategies

| Sub-client | Strategy | Why |
|---|---|---|
| Conversations (Activities, Members, Reactions) | Delegates to core `ConversationClient` | Reuses auth, logging, agents-channel handling, agentic identity support |
| Bots.SignIn, Users.Token | Delegates to core `UserTokenClient` | Reuses auth, logging, agentic identity; single source of truth for token API calls |
| Teams, Meetings | Uses `BotHttpClient` directly | No core client equivalent exists for these endpoints |

### Experimental APIs

| Feature | Diagnostic ID | Notes |
|---|---|---|
| `ReactionClient` | `ExperimentalTeamsReactions` | Reactions endpoint assumed but not confirmed in Teams Bot Framework API |
| `ActivityClient.CreateTargetedAsync` / `UpdateTargetedAsync` / `DeleteTargetedAsync` | `ExperimentalTeamsTargeted` | Targeted (recipient-only visible) activities; not supported in team channel conversations |

## Construction & Scoping

### The serviceUrl problem

The Bot Framework service URL is per-request (comes from `activity.ServiceUrl`), but `ApiClient` is per-application (DI singleton). The `ApiClient` solves this with a two-step pattern:

1. **DI registration** creates a base `ApiClient` without a serviceUrl
2. **Per-request**, `ForServiceUrl(uri)` creates a lightweight scoped copy with all sub-clients bound

### DI-friendly constructor (no serviceUrl)

```csharp
// Registered automatically by AddTeamsBotApplication()
// The [ActivatorUtilitiesConstructor] attribute tells DI to prefer this constructor
[ActivatorUtilitiesConstructor]
public ApiClient(HttpClient httpClient, ConversationClient conversationClient, UserTokenClient userTokenClient, ILogger? logger = null)
```

`AddTeamsBotApplication()` calls `AddBotClient<ApiClient>(...)` which registers `ApiClient` as a typed HTTP client with `BotAuthenticationHandler`. The `ConversationClient` and `UserTokenClient` dependencies are resolved from DI automatically.

**Important:** The base `ApiClient` has `Conversations`, `Teams`, and `Meetings` set to `null!`. Only `Bots` and `Users` are available on the unscoped instance. Accessing `Conversations`, `Teams`, or `Meetings` directly causes `NullReferenceException`. Always use `ForServiceUrl()` or `Context.Api` to get a scoped instance.

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

`ForServiceUrl` shares the underlying `BotHttpClient`, `ConversationClient`, and `UserTokenClient` — only the sub-client wrappers are new allocations.

### Constructors

| Constructor | Use case |
|---|---|
| `ApiClient(HttpClient, ConversationClient, UserTokenClient, ILogger?)` | DI registration (marked `[ActivatorUtilitiesConstructor]`) |
| `ApiClient(Uri, HttpClient, ConversationClient, UserTokenClient, ILogger?)` | Fully initialized with known serviceUrl |
| `ApiClient(ApiClient)` | Copy constructor |
| Private: `ApiClient(BotHttpClient, ConversationClient, UserTokenClient, Uri)` | Used by `ForServiceUrl` — shares clients |

## Delegation Pattern

The Apps-layer sub-clients delegate to core clients rather than duplicating HTTP logic:

- **Conversation sub-clients** (`ActivityClient`, `MemberClient`, `ReactionClient`) → core `ConversationClient`
- **Token/SignIn sub-clients** (`UserTokenApiClient`, `BotSignInClient`) → core `UserTokenClient`

This ensures:

- Single source of truth for URL construction, auth, and error handling
- Agents-channel ID truncation logic is preserved
- Agentic identity support works transparently for all operations
- Custom headers and logging from core clients apply

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
| `CreateTargetedAsync(conversationId, activity)` | `SendActivityAsync(activity)` | Sets `Recipient.IsTargeted = true` [Experimental] |
| `UpdateTargetedAsync(conversationId, id, activity)` | `UpdateTargetedActivityAsync(conversationId, id, activity)` | Sets `ServiceUrl` [Experimental] |
| `DeleteTargetedAsync(conversationId, id)` | `DeleteTargetedActivityAsync(conversationId, id, serviceUrl)` | [Experimental] |

#### MemberClient → ConversationClient

| MemberClient | ConversationClient |
|---|---|
| `GetAsync(conversationId)` | `GetConversationMembersAsync(conversationId, serviceUrl)` |
| `GetByIdAsync(conversationId, memberId)` | `GetConversationMemberAsync<ConversationAccount>(conversationId, memberId, serviceUrl)` |
| `GetByIdAsync<T>(conversationId, memberId)` | `GetConversationMemberAsync<T>(conversationId, memberId, serviceUrl)` |
| `DeleteAsync(conversationId, memberId)` | `DeleteConversationMemberAsync(conversationId, memberId, serviceUrl)` |

#### ReactionClient → ConversationClient [Experimental]

| ReactionClient | ConversationClient |
|---|---|
| `AddAsync(conversationId, activityId, reactionType)` | `AddReactionAsync(conversationId, activityId, reactionType, serviceUrl)` |
| `DeleteAsync(conversationId, activityId, reactionType)` | `DeleteReactionAsync(conversationId, activityId, reactionType, serviceUrl)` |

#### ConversationApiClient → ConversationClient

| ConversationApiClient | ConversationClient |
|---|---|
| `CreateAsync(parameters)` | `CreateConversationAsync(parameters, serviceUrl)` |

#### TeamClient (direct HTTP)

| TeamClient | Endpoint |
|---|---|
| `GetByIdAsync(id)` | `GET {serviceUrl}/v3/teams/{id}` |
| `GetConversationsAsync(id)` | `GET {serviceUrl}/v3/teams/{id}/conversations` |

#### MeetingClient (direct HTTP)

| MeetingClient | Endpoint |
|---|---|
| `GetByIdAsync(id)` | `GET {serviceUrl}/v1/meetings/{id}` |
| `GetParticipantAsync(meetingId, id, tenantId)` | `GET {serviceUrl}/v1/meetings/{meetingId}/participants/{id}?tenantId={tenantId}` |

#### BotSignInClient → UserTokenClient

| BotSignInClient | UserTokenClient |
|---|---|
| `GetUrlAsync(state, codeChallenge?, emulatorUrl?, finalRedirect?)` | `GetSignInUrlAsync(state, codeChallenge?, emulatorUrl?, finalRedirect?)` |
| `GetResourceAsync(state, codeChallenge?, emulatorUrl?, finalRedirect?)` | `GetSignInResourceAsync(state, codeChallenge?, emulatorUrl?, finalRedirect?)` |

#### UserTokenApiClient → UserTokenClient

| UserTokenApiClient | UserTokenClient | Notes |
|---|---|---|
| `GetAsync(userId, connectionName, channelId, code?)` | `GetTokenAsync(userId, connectionName, channelId, code?)` | |
| `GetAadAsync(userId, connectionName, channelId, resourceUrls?)` | `GetAadTokensAsync(userId, connectionName, channelId, resourceUrls?)` | `IList<string>?` → `string[]?` |
| `GetStatusAsync(userId, channelId, includeFilter?)` | `GetTokenStatusAsync(userId, channelId, include?)` | Returns `GetTokenStatusResult[]` as `IList<>?` |
| `SignOutAsync(userId, connectionName, channelId)` | `SignOutUserAsync(userId, connectionName?, channelId?)` | |
| `ExchangeAsync(userId, connectionName, channelId, token)` | `ExchangeTokenAsync(userId, connectionName, channelId, token?)` | |

## File Layout

```
core/src/Microsoft.Teams.Apps/Api/Clients/
├── ApiClient.cs              Top-level facade, DI entry point, ForServiceUrl factory
├── ConversationApiClient.cs  Conversation facade → delegates to core ConversationClient
├── ActivityClient.cs         Activity CRUD + targeted → delegates to core ConversationClient
├── MemberClient.cs           Member operations → delegates to core ConversationClient
├── ReactionClient.cs         Reaction operations → delegates to core ConversationClient [Experimental]
├── TeamClient.cs             Team info → BotHttpClient (v3/teams/)
├── MeetingClient.cs          Meeting info → BotHttpClient (v1/meetings/) + models
├── BotClient.cs              Bot facade (groups SignIn)
├── BotSignInClient.cs        Sign-in URLs → delegates to core UserTokenClient
├── BotTokenClient.cs         Static scope constants
├── UserClient.cs             User facade (groups Token)
└── UserTokenApiClient.cs     User token ops → delegates to core UserTokenClient
```

## Integration with Context and Handlers

The `Context<TActivity>` class exposes a lazy `Api` property:

```csharp
public ApiClient Api => _api ??= TeamsBotApplication.Api.ForServiceUrl(Activity.ServiceUrl);
```

This is the primary way handlers should access the API clients. It ensures the scoped `ApiClient` is created once per request and reused across multiple calls within the same handler.

## Integration with TeamsApiClient

`TeamsApiClient` retrieves clients from `TurnState`:

- **`ApiClient`** (from `TurnState.Get<ApiClient>()`) for Teams/Meetings operations:
  - `client.Teams.GetByIdAsync(teamId)` — team details
  - `client.Teams.GetConversationsAsync(teamId)` — channel list
  - `client.Meetings.GetParticipantAsync(meetingId, participantId, tenantId)` — meeting participant

- **`ConversationClient`** (from `CompatConnectorClient` in `TurnState.Get<IConnectorClient>()`) for member operations:
  - `GetConversationMemberAsync<TeamsConversationAccount>(...)` — single member
  - `GetConversationMembersAsync(...)` — all members
  - `GetConversationPagedMembersAsync(...)` — paged members

**Note on TeamsBotFrameworkHttpAdapter scoping:** The `TeamsBotFrameworkHttpAdapter` currently stores the unscoped `TeamsApiClient` in `TurnState` (line 59). This works because `TeamsApiClient` uses the `ApiClient` sub-clients which are scoped. However, `TeamsBotFrameworkHttpAdapter` should ideally scope the `ApiClient` before storing:

```csharp
// Current (unscoped — Teams/Meetings sub-clients are null):
turnContext.TurnState.Add<ApiClient>(_teamsBotApplication.TeamsApiClient);

// Should be (scoped):
ApiClient scopedClient = _teamsBotApplication.TeamsApiClient.ForServiceUrl(new Uri(activity.ServiceUrl));
turnContext.TurnState.Add<ApiClient>(scopedClient);
```

## Future Work

- **BatchClient**: Batch messaging operations (`SendMessageToListOfUsersAsync`, etc.) need a new sub-client on `ApiClient` using `BotHttpClient` for the `v3/batch/conversation/` endpoints.
- **MeetingClient.SendMeetingNotificationAsync**: Meeting notification support needs to be added along with notification model types.
- **TeamsBotFrameworkHttpAdapter scoping**: Fix `TeamsBotFrameworkHttpAdapter` to call `ForServiceUrl` before storing `ApiClient` in `TurnState`.
