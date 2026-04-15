# Breaking Changes: Microsoft.Teams.Api.Clients → Microsoft.Teams.Bot.Apps.Api.Clients

**Date:** 2026-04-15
**Status:** Review

## Overview

This document catalogs all breaking changes when migrating from the old `Libraries/Microsoft.Teams.Api/Clients/` to the new backward-compatible wrapper classes in `core/src/Microsoft.Teams.Bot.Apps/Api/Clients/`.

The wrapper classes preserve **class names** and **method names** from the old SDK but use new SDK types internally. The goal is to minimize migration effort while enabling new features like `AgenticIdentity`.

## AgenticIdentity Support (New Feature, Non-Breaking)

The old SDK had no concept of `AgenticIdentity`. The new wrapper classes support it via:

1. **Instance-level default** — Set on `ApiClient` at creation, flows to all sub-clients
2. **Per-call optional param** — Last optional parameter on every method (`AgenticIdentity? agenticIdentity = null`)
3. **Resolution** — `per-call ?? instance-default ?? null`

```csharp
// Old code — still compiles, no AgenticIdentity:
await api.Conversations.Activities.CreateAsync(convId, activity);

// New code — instance-level:
var api = factory.Create(serviceUrl, agenticIdentity);
await api.Conversations.Activities.CreateAsync(convId, activity);

// New code — per-call override:
await api.Conversations.Activities.CreateAsync(convId, activity, agenticIdentity: myIdentity);
```

## Breaking Changes by Category

### 1. Type Changes

| Old Type | New Type | Impact |
|---|---|---|
| `IActivity` | `CoreActivity` | All activity parameters. Different type hierarchy, different serialization. |
| `Account` | `ConversationAccount` | Member/user references. Property names mostly align. |
| `Resource` (`.Id`) | `SendActivityResponse?` (`.Id`) | Return type for send/create. `.Id` property preserved. |
| `Resource` (`.Id`) | `UpdateActivityResponse` (`.Id`) | Return type for update. `.Id` property preserved. |
| `ConversationResource` | `CreateConversationResponse` | Return type for create conversation. `.Id`, `.ActivityId` preserved. |
| `Team` | `TeamDetails` | Team info. Similar properties but different type name. |
| `List<Channel>` | `ChannelList` | Channel list. Access via `.Channels` property instead of directly. |
| `Meeting` | `MeetingInfo` | Meeting info. Different nested structure. |
| `MeetingParticipant` (old) | `MeetingParticipant` (new) | Same name, different namespace/properties. |
| `Token.Response` | `GetTokenResult?` | Token results. `.Token` property preserved. |
| `IList<Token.Status>` | `GetTokenStatusResult[]` | Token status. Array instead of IList. |
| `SignIn.UrlResponse` | `GetSignInResourceResult` | Sign-in resource. `.SignInLink` instead of `.SignInLink`. |
| `ReactionType` (enum) | `string` | Reaction type parameter. Enum values → string literals (e.g., `"like"`, `"laugh"`). |

### 2. Constructor Changes

| Old Pattern | New Pattern | Migration |
|---|---|---|
| `new ApiClient(serviceUrl, httpClient)` | `factory.Create(serviceUrl)` | Use `ApiClientFactory` from DI |
| `new ApiClient(serviceUrl, httpClientOptions)` | `factory.Create(serviceUrl)` | DI handles HTTP configuration |
| `new ApiClient(serviceUrl, httpClientFactory)` | `factory.Create(serviceUrl)` | DI handles HTTP client creation |
| `serviceUrl` as `string` | `serviceUrl` as `Uri` | Wrap in `new Uri(serviceUrl)` |

### 3. Method Signature Changes

**Parameters preserved (method name + parameter names unchanged):**
- `ActivityClient.CreateAsync(conversationId, activity)` ✓
- `ActivityClient.UpdateAsync(conversationId, id, activity)` ✓
- `ActivityClient.ReplyAsync(conversationId, id, activity)` ✓
- `ActivityClient.DeleteAsync(conversationId, id)` ✓
- `ActivityClient.CreateTargetedAsync(conversationId, activity)` ✓
- `ActivityClient.UpdateTargetedAsync(conversationId, id, activity)` ✓
- `ActivityClient.DeleteTargetedAsync(conversationId, id)` ✓
- `MemberClient.GetAsync(conversationId)` ✓
- `MemberClient.GetByIdAsync(conversationId, memberId)` ✓
- `MemberClient.DeleteAsync(conversationId, memberId)` ✓
- `ReactionClient.AddAsync(conversationId, activityId, reactionType)` ✓ (type changed to string)
- `ReactionClient.DeleteAsync(conversationId, activityId, reactionType)` ✓ (type changed to string)
- `ConversationClient.CreateAsync(request)` ✓ (request type changed)
- `TeamClient.GetByIdAsync(id)` ✓
- `TeamClient.GetConversationsAsync(id)` ✓
- `MeetingClient.GetByIdAsync(id)` ✓
- `MeetingClient.GetParticipantAsync(meetingId, id, tenantId)` ✓

**Parameters changed (flattened from request objects):**
- `UserTokenClient.GetAsync(request)` → `GetAsync(userId, connectionName, channelId, code?)`
- `UserTokenClient.GetAadAsync(request)` → `GetAadAsync(userId, connectionName, channelId, resourceUrls?)`
- `UserTokenClient.GetStatusAsync(request)` → `GetStatusAsync(userId, channelId, include?)`
- `UserTokenClient.SignOutAsync(request)` → `SignOutAsync(userId, connectionName?, channelId?)`
- `UserTokenClient.ExchangeAsync(request)` → `ExchangeAsync(userId, connectionName, channelId, exchangeToken)`
- `BotSignInClient.GetResourceAsync(request)` → `GetResourceAsync(userId, connectionName, channelId, finalRedirect?)`

### 4. Removed / Deprecated

| Old Feature | Status | Migration Path |
|---|---|---|
| `BotTokenClient.GetAsync(credentials)` | `[Obsolete]` stub | Auth handled by DI pipeline (`BotAuthenticationHandler`) |
| `BotTokenClient.GetGraphAsync(credentials)` | `[Obsolete]` stub | Auth handled by DI pipeline |
| `BotSignInClient.GetUrlAsync(request)` | `[Obsolete]`, throws `NotSupportedException` | Use `BotSignInClient.GetResourceAsync()` → `.SignInLink` |
| `Client` base class | Removed | No equivalent needed — wrapper delegates to DI-injected clients |
| `IHttpClient` / `IHttpClientFactory` constructors | Removed | Use `ApiClientFactory` from DI |
| Request nested classes (`GetTokenRequest`, etc.) | Removed | Parameters flattened into method signatures |

### 5. Structural Changes

| Old Structure | New Structure | Notes |
|---|---|---|
| `ApiClient.Bots.Token` → `BotTokenClient` | `ApiClient.Bots.Token` → stub | Auth handled differently |
| `ApiClient.Bots.SignIn` → `BotSignInClient` | `ApiClient.Bots.SignIn` → `BotSignInClient` | Delegates to `Core.UserTokenClient` |
| `ApiClient.Client` (IHttpClient property) | Removed | No raw HTTP access — use SDK methods |

### 6. Namespace Change

```
Old: Microsoft.Teams.Api.Clients
New: Microsoft.Teams.Bot.Apps.Api.Clients
```

Migration: Update `using` statements.

**Name collision note:** `ConversationClient` and `UserTokenClient` exist in both `Microsoft.Teams.Bot.Core` (the actual SDK client) and `Microsoft.Teams.Bot.Apps.Api.Clients` (the backward-compat wrapper). Use fully qualified names or aliases when both are in scope.

## What's Preserved

- All class names (ApiClient, ActivityClient, MemberClient, ReactionClient, ConversationClient, TeamClient, MeetingClient, UserTokenClient, BotSignInClient, BotTokenClient, BotClient, UserClient)
- All method names
- Hierarchical structure (ApiClient.Conversations.Activities.CreateAsync)
- Property names on sub-client accessors (.Activities, .Members, .Reactions, .Token, .SignIn)
- serviceUrl stored at client level (not per-call)
- Optional CancellationToken on all methods

## Migration Checklist

- [ ] Update package references (old Libraries → new core)
- [ ] Update `using` statements (`Microsoft.Teams.Api.Clients` → `Microsoft.Teams.Bot.Apps.Api.Clients`)
- [ ] Replace `new ApiClient(serviceUrl, ...)` with `factory.Create(new Uri(serviceUrl))`
- [ ] Replace `IActivity` with `CoreActivity` in activity parameters
- [ ] Replace `Account` with `ConversationAccount`
- [ ] Replace `Resource` return types with `SendActivityResponse` / `UpdateActivityResponse`
- [ ] Replace `ReactionType` enum with string literals
- [ ] Flatten `UserTokenClient` request objects into direct parameters
- [ ] Replace `BotTokenClient` auth calls with DI-based auth
- [ ] Replace `BotSignInClient.GetUrlAsync` with `GetResourceAsync().SignInLink`
