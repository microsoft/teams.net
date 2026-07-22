# ApiClient Design

## Overview

`ApiClient` (`Microsoft.Teams.Apps.Clients`) is a hierarchical, Libraries-compatible facade over the Bot Framework v3 REST API. Sub-clients delegate to the core SDK (`ConversationClient`, `UserTokenClient`) or call `BotHttpClient` directly — they never duplicate HTTP logic.

## Architecture

```
ApiClient (top-level facade)
├── Conversations     → ConversationApiClient   [→ core ConversationClient]
│   ├── Activities    → ActivityClient
│   ├── Members       → MemberClient
│   └── Reactions     → ReactionClient           [Experimental]
├── Users             → UserTokenApiClient        [token + sign-in; → core UserTokenClient]
├── Teams             → TeamClient                [BotHttpClient → serviceUrl/v3/teams/]
└── Meetings          → MeetingClient             [BotHttpClient → serviceUrl/v1/meetings/]
```

| Sub-client | Backed by | Why |
|---|---|---|
| Conversations, Users | core `ConversationClient` / `UserTokenClient` | Single source of truth for URL construction, auth, agents-channel handling, agentic identity, headers, and logging |
| Teams, Meetings | `BotHttpClient` directly | No core client exists for these endpoints |

**Experimental APIs:** `ReactionClient` (`ExperimentalTeamsReactions`); `ActivityClient.CreateTargetedAsync` / `UpdateTargetedAsync` / `DeleteTargetedAsync` (`ExperimentalTeamsTargeted`, not supported in team channels).

## Construction & scoping

The service URL is per-request (`activity.ServiceUrl`), but `ApiClient` is a DI singleton. This is resolved in two steps:

1. **DI** registers a named, authenticated `HttpClient` (`AddBotHttpClient(nameof(ApiClient), …)` with `BotAuthenticationHandler`) and a singleton factory that builds the base `ApiClient` (no serviceUrl) from it, injecting `ConversationClient` and `UserTokenClient`. `ApiClient` wraps the `HttpClient` in a `BotHttpClient` internally — the same pattern as the core `ConversationClient` / `UserTokenClient`.
2. **Per request**, `ForServiceUrl(uri)` returns a lightweight scoped copy — it shares the underlying `BotHttpClient`, `ConversationClient`, and `UserTokenClient`; only the sub-client wrappers are new allocations.

In handlers, use `ctx.Api`, which lazily scopes to `Activity.ServiceUrl`:

```csharp
botApp.OnMessage(async (ctx, ct) =>
{
    var members = await ctx.Api.Conversations.Members.GetAsync(conversationId, ct);
    var team = await ctx.Api.Teams.GetByIdAsync(teamId, ct);
});
```

> ⚠️ On the **unscoped** base client, `Conversations`, `Teams`, and `Meetings` are `null!` — only `Users` is usable. Never call `ctx.TeamsBotApplication.Api.Conversations` directly (it throws `NullReferenceException`). Outside handlers (proactive messaging, compat layer), scope explicitly with `ForServiceUrl(activity.ServiceUrl)`.

Constructors: a DI constructor `(HttpClient, ConversationClient, UserTokenClient, ILogger?)` marked `[ActivatorUtilitiesConstructor]`, a fully-initialized `(Uri, …)` variant, a copy constructor, and a private `ForServiceUrl` constructor that shares the underlying clients.

## Delegation

Sub-clients are thin adapters: they bridge the Libraries-style `(conversationId, activity)` signature to the core clients — setting `ServiceUrl`, `Conversation`, and `ReplyToId` as needed — then forward. `Teams` and `Meetings` have no core equivalent, so they issue `BotHttpClient` calls against `v3/teams/` and `v1/meetings/`. See the source for exact per-method mappings.

## File layout

```
core/src/Microsoft.Teams.Apps/Api/Clients/
├── ApiClient.cs              Facade, DI entry point, ForServiceUrl factory
├── ConversationApiClient.cs  Conversation facade → core ConversationClient
├── ActivityClient.cs         Activity CRUD + targeted → core ConversationClient
├── MemberClient.cs           Member operations → core ConversationClient
├── ReactionClient.cs         Reaction operations → core ConversationClient [Experimental]
├── TeamClient.cs             Team info → BotHttpClient (v3/teams/)
├── MeetingClient.cs          Meeting info → BotHttpClient (v1/meetings/) + models
└── UserTokenApiClient.cs     User token + sign-in → core UserTokenClient
```

## Integration

`Context.Api` is the primary entry point for handlers — lazily scoped and reused per request:

```csharp
public ApiClient Api => _api ??= TeamsBotApplication.Api.ForServiceUrl(Activity.ServiceUrl);
```

The compat `TeamsApiClient` pulls `ApiClient` (Teams/Meetings ops) and `ConversationClient` (member ops) from `TurnState`.

## Future work

- **BatchClient** — a sub-client over `BotHttpClient` for `v3/batch/conversation/` endpoints.
- **MeetingClient.SendMeetingNotificationAsync** — plus notification model types.
- **TeamsBotFrameworkHttpAdapter scoping** — call `ForServiceUrl` before storing `ApiClient` in `TurnState` (it currently stores the unscoped client, so Teams/Meetings sub-clients are `null`).
