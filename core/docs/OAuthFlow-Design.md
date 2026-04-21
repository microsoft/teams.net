# OAuthFlow Design Document

## Overview

`OAuthFlow` provides a high-level abstraction for Teams Bot SSO (Single Sign-On) authentication. It encapsulates the full OAuth lifecycle -- silent token acquisition, SSO token exchange, fallback sign-in, and sign-out -- so developers can add user authentication with minimal plumbing.

The design builds on top of the existing `UserTokenClient` (core) and `UserTokenApiClient` / `BotSignInClient` (Apps layer), and follows the handler-based routing pattern established by `AdaptiveCardExtensions`, `TaskExtensions`, etc.

## Motivation

Teams SSO requires coordinating multiple moving parts:

1. Checking the Bot Framework Token Store for an existing token
2. Sending an OAuthCard with a `TokenExchangeResource` to trigger silent SSO
3. Handling `signin/tokenExchange` invoke activities (with deduplication)
4. Handling `signin/verifyState` invoke activities (fallback sign-in flow)
5. Handling magic codes arriving as plain messages (non-AAD providers)
6. Calling `UserTokenClient.ExchangeTokenAsync` to complete the on-behalf-of exchange

Without an abstraction, every bot developer must wire this up manually. `OAuthFlow` reduces it to a few method calls.

## Architecture

```
TeamsBotApplication
├── AppId                                  ← from BotConfig.ClientId
├── OAuthRegistry                          ← holds all OAuthFlow instances
├── Router
│   ├── ... existing routes ...
│   ├── message/oauth/magicCode            ← registered by OAuthFlow (magic code interception)
│   ├── invoke/signin/tokenExchange        ← registered by OAuthFlow
│   └── invoke/signin/verifyState          ← registered by OAuthFlow
└── OAuthFlow (one per connection)
    ├── SignInAsync()        → silent token check + OAuthCard
    ├── SignOutAsync()       → revoke token
    ├── IsSignedInAsync()    → check token store
    ├── GetTokenAsync()      → silent-only token retrieval
    ├── OnSignInComplete()   → callback after successful exchange
    └── OnSignInFailure()    → callback on exchange failure
```

### Two API Layers

Developers can use **either** the context-level API (simple, matches Teams SDK v2 pattern) or the OAuthFlow-instance API (advanced, explicit per-connection control):

| Scenario | Context API (simple) | OAuthFlow API (advanced) |
|---|---|---|
| Sign in | `context.SignIn(new OAuthOptions { ConnectionName = "gh" })` | `githubAuth.SignInAsync(context)` |
| Sign out | `context.SignOut("gh")` | `githubAuth.SignOutAsync(context)` |
| Check status | `context.IsSignedInAsync("gh")` | `githubAuth.IsSignedInAsync(context)` |
| All connections | `context.GetConnectionStatusAsync()` | `graphAuth.GetConnectionStatusAsync(context)` |
| Single connection | `context.SignIn()` / `context.IsSignedIn` | `auth.SignInAsync(context)` |

### Relationship to existing clients

```
OAuthFlow (Apps layer - developer-facing)
    │
    ├── UserTokenClient.GetTokenAsync()              → silent token check
    ├── UserTokenClient.ExchangeTokenAsync()         → SSO token exchange
    ├── UserTokenClient.GetTokenStatusAsync()        → connection discovery & status
    ├── UserTokenClient.SignOutUserAsync()            → sign-out
    └── UserTokenClient.GetSignInResourceAsync()     → sign-in resource (OAuthCard data)
```

`OAuthFlow` does **not** replace these clients. It orchestrates them into a cohesive flow and auto-registers the invoke handlers that the SSO protocol requires.

## Breaking Changes from Teams SDK v2 (Spark)

This section documents every API and behavioral difference between the old `Context<TActivity>` (in `Microsoft.Teams.Apps`) and the new `Context<TActivity>` (in `Microsoft.Teams.Bot.Apps`) related to SSO/Auth.

### 1. `context.ConnectionName` removed

**Old (v2)**: `Context<TActivity>` has a `required string ConnectionName` property that holds the app's default connection name (set during context construction, defaults to `"graph"`). `SignIn()` and `SignOut()` fall back to this when no explicit connection name is given.

**New**: No `ConnectionName` property on context. The default connection is resolved from the `OAuthFlowRegistry` -- if a single `OAuthFlow` is registered, it is used as the default. If multiple are registered, the developer must specify the connection name per-call.

```csharp
// Old (v2) -- default connection baked into context
await context.SignIn(); // uses context.ConnectionName ("graph")

// New -- resolved from OAuthFlowRegistry
bot.AddOAuthFlow("graph"); // single flow → becomes the default
await context.SignIn();    // works (single flow auto-resolves)

// New -- multiple flows, must be explicit
bot.AddOAuthFlow("graph");
bot.AddOAuthFlow("gh");
await context.SignIn(new OAuthOptions { ConnectionName = "gh" });
```

**Migration**: Replace reads of `context.ConnectionName` with the explicit connection name in `OAuthOptions` or `SignOut(connectionName)`.

### 2. `context.IsSignedIn` semantics changed

**Old (v2)**: `IsSignedIn` is a read/write `bool` property (`{ get; set; }`). It is set to `true` by the framework when a `signin/tokenExchange` invoke completes successfully during the current turn. It is a **per-turn flag**, not a token-store query. It reflects whether the sign-in **just happened** in this turn, not whether a token exists in the store.

**New**: `IsSignedIn` is a read-only `bool` property that **synchronously queries the token store** (`GetAwaiter().GetResult()`). It checks whether the user has a cached token right now, regardless of what happened during this turn. It cannot be set by the developer.

| | Old (v2) | New |
|---|---|---|
| Type | `bool { get; set; }` | `bool { get; }` |
| Source of truth | Framework sets it during the turn | Queries token store on each access |
| Async | No (already computed) | No (sync-over-async) |
| Multi-connection | N/A (one default connection) | Checks first registered flow, logs warning if multiple |
| Writable | Yes | No |

**Recommended migration**: Use `IsSignedInAsync(connectionName?)` for async, connection-aware checks:

```csharp
// Old (v2)
if (!context.IsSignedIn) { await context.SignIn(); return; }

// New (preferred)
if (!await context.IsSignedInAsync("graph", ct)) { await context.SignIn(new OAuthOptions { ConnectionName = "graph" }, ct); return; }

// New (backwards-compat, single connection only)
if (!context.IsSignedIn) { await context.SignIn(ct); return; }
```

### 3. `context.UserGraphToken` removed

**Old (v2)**: `Context<TActivity>` has a `JsonWebToken? UserGraphToken` property set by the framework's `OnTokenExchangeActivity` handler after a successful token exchange. It provides parsed JWT access to the Graph token (claims, expiry, etc.).

**New**: No `UserGraphToken` property. The token is returned as a raw `string` from `SignIn()` / `GetTokenAsync()` / `OnSignInComplete`. If JWT parsing is needed, the developer must parse it themselves.

```csharp
// Old (v2)
var graphClient = new SimpleGraphClient(context.UserGraphToken?.ToString()!);

// New
string? token = await context.SignIn(new OAuthOptions { ConnectionName = "graph" }, ct);
var graphClient = new SimpleGraphClient(token!);
```

### 4. `context.SignIn(SSOOptions)` overload removed

**Old (v2)**: Two `SignIn` overloads exist:
- `SignIn(OAuthOptions?)` -- OAuth flow via Bot Framework Token Service
- `SignIn(SSOOptions)` -- Direct SSO flow with custom scopes and sign-in link (bypasses Token Service, constructs its own `TokenExchangeResource`)

**New**: Only `SignIn(OAuthOptions?)` is available. The SSO flow is handled transparently when the OAuth connection is configured as Azure AD v2 -- the `TokenExchangeResource` is returned by the Token Service when `MsAppId` is included in the state.

**Migration**: Remove `SSOOptions` usage. Configure the OAuth connection in Azure Bot settings with the appropriate scopes. The `OAuthFlow` handles SSO automatically for Azure AD connections.

### 5. `context.SignIn()` return type is the same but semantics differ

**Old (v2)**: `SignIn(OAuthOptions?)` returns `Task<string?>`. Returns the cached token if found, otherwise sends OAuthCard and returns `null`. The `SignIn(SSOOptions)` overload returns `Task` (void).

**New**: `SignIn(OAuthOptions?)` returns `Task<string?>` with the same semantics -- token if cached, `null` if OAuthCard sent. No void overload.

This is **API-compatible** for the `OAuthOptions` overload. Breaking only for `SSOOptions` users.

### 6. `OnSignInComplete` callback signature

**Old (v2)**: Sign-in success is delivered via an app-level event:
```csharp
// Old (v2)
teams.OnSignIn(async (plugin, @event, cancellationToken) => {
    var token = @event.Token;                    // Token.Response object
    var context = @event.Context;                // IContext<SignInActivity>
});
```

**New**: Sign-in success is delivered via a per-connection callback:
```csharp
// New
graphAuth.OnSignInComplete(async (context, tokenResponse, ct) => {
    string token = tokenResponse.Token!;         // GetTokenResult
    // context is Context<TeamsActivity> (base type)
});
```

Key differences:
- **Scope**: Old is app-level (one handler for all connections). New is per-connection.
- **Context type**: Old provides `IContext<SignInActivity>`. New provides `Context<TeamsActivity>` because the sign-in can complete from invoke (tokenExchange, verifyState) or message (magic code) activities.
- **Token type**: Old provides `Token.Response` (with `ConnectionName`, `Token`, `Expiration`, `Properties`). New provides `GetTokenResult` (with `ConnectionName`, `Token`).
- **Plugin parameter**: Old receives the plugin instance. New does not -- the context has access to `TeamsBotApplication`.

### 7. `OnSignInFailure` callback signature and scope

**Old (v2)**: App-level handler receiving the failure activity:
```csharp
// Old (v2)
teams.OnSignInFailure(async (context, cancellationToken) => {
    var failure = context.Activity.Value; // SignIn.Failure { Code, Message }
    await context.Send("Sign-in failed.", cancellationToken);
});
```

**New**: Per-connection handler on the `OAuthFlow` instance:
```csharp
// New
graphAuth.OnSignInFailure(async (context, ct) => {
    // context is Context<TeamsActivity>
    await context.SendActivityAsync("Sign-in failed.", ct);
});
```

Key differences:
- **Scope**: Per-connection instead of app-level.
- **Failure details**: Old provides `SignIn.Failure` with `Code` and `Message` via the activity value. New does not expose structured failure details (the failure is logged internally).
- **`context.Send` → `context.SendActivityAsync`**: Method name change (see below).

### 8. `context.Send()` → `context.SendActivityAsync()`

**Old (v2)**: `context.Send(string)` and `context.Send<T>(T activity)`.

**New**: `context.SendActivityAsync(string)` and `context.SendActivityAsync(TeamsActivity)`.

This affects all code inside `OnSignInComplete` and `OnSignInFailure` callbacks.

### 9. Group chat handling removed from `SignIn`

**Old (v2)**: `Context.SignIn()` detects group chats (`Activity.Conversation.IsGroup == true`) and automatically creates a 1:1 conversation with the user before sending the OAuthCard, because group chats don't support SSO.

**New**: `OAuthFlow.SignInAsync()` does not handle the group-chat-to-1:1 conversion. The OAuthCard is sent to the current conversation. For group chats, the sign-in card will show the button (no SSO), but the popup flow still works.

**Migration**: If group chat SSO is required, the developer must create the 1:1 conversation manually before calling `context.SignIn()`.

### 10. `OAuthOptions` namespace and defaults

| | Old (v2) | New |
|---|---|---|
| Namespace | `Microsoft.Teams.Apps` | `Microsoft.Teams.Bot.Apps.Auth` |
| Base class | `SignInOptions` (abstract) | None (standalone class) |
| `OAuthCardText` default | `"Please Sign In..."` | `"Please Sign In"` |
| `SignInButtonText` default | `"Sign In"` | `"Sign In"` |
| `ConnectionName` | Falls back to `context.ConnectionName` | Falls back to single registered `OAuthFlow` |

### 11. `SSOOptions` class removed

**Old (v2)**: `SSOOptions : SignInOptions` with `required string[] Scopes` and `required string SignInLink`.

**New**: Not available. SSO is handled automatically for Azure AD connections via the `TokenExchangeResource` mechanism.

### 12. No `context.Next()` equivalent in auth handlers

**Old (v2)**: `context.Next()` continues the middleware/route chain. The `OnSignIn` event handler can call `context.Next()` to continue processing.

**New**: `OnSignInComplete` and `OnSignInFailure` are terminal callbacks, not middleware. They do not participate in the route chain.

### Summary Table

| Feature | Old (v2) `Microsoft.Teams.Apps` | New `Microsoft.Teams.Bot.Apps` | Breaking? |
|---|---|---|---|
| `context.ConnectionName` | `required string` property | Removed (resolved from registry) | Yes |
| `context.IsSignedIn` | `bool { get; set; }` (per-turn flag) | `bool { get; }` (queries token store) | Yes (semantic) |
| `context.UserGraphToken` | `JsonWebToken?` property | Removed | Yes |
| `context.SignIn(OAuthOptions?)` | Returns `Task<string?>` | Returns `Task<string?>` | No |
| `context.SignIn(SSOOptions)` | Returns `Task` | Removed | Yes |
| `context.SignOut(string?)` | Returns `Task` | Returns `Task` | No |
| `OnSignIn` event | App-level, `SignInEvent` | Per-connection `OnSignInComplete` | Yes |
| `OnSignInFailure` event | App-level, `SignIn.Failure` | Per-connection `OnSignInFailure` | Yes |
| `OAuthOptions` namespace | `Microsoft.Teams.Apps` | `Microsoft.Teams.Bot.Apps.Auth` | Yes |
| `SSOOptions` | Available | Removed | Yes |
| Group chat 1:1 fallback | Automatic | Manual | Yes (behavioral) |
| `context.Send()` | Available | `context.SendActivityAsync()` | Yes (rename) |
| `context.Next()` in auth | Available | Not applicable | Yes |
| `IsSignedInAsync()` | Not available | New method | N/A (addition) |
| `GetConnectionStatusAsync()` | Not available | New method | N/A (addition) |

## API Surface

### Registration

```csharp
public static class OAuthFlowExtensions
{
    /// Register an OAuthFlow with an explicit connection name.
    public static OAuthFlow AddOAuthFlow(this TeamsBotApplication app, string connectionName);

    /// Register an OAuthFlow that auto-discovers the connection name
    /// via GetTokenStatus on first use.
    public static OAuthFlow AddOAuthFlow(this TeamsBotApplication app);
}
```

`AddOAuthFlow` registers three routes on the app's `Router`:

| Route name | Activity type | Purpose |
|---|---|---|
| `message/oauth/magicCode` | Message (4-8 digit text) | Magic code interception for non-AAD providers |
| `invoke/signin/tokenExchange` | Invoke | SSO silent token exchange |
| `invoke/signin/verifyState` | Invoke | Fallback sign-in verification |

### Context Methods

```csharp
public class Context<TActivity> where TActivity : TeamsActivity
{
    /// Trigger sign-in flow. Returns cached token or null if OAuthCard sent.
    public Task<string?> SignIn(OAuthOptions? options = null, CancellationToken ct = default);

    /// Sign the user out from a connection.
    public Task SignOut(string? connectionName = null, CancellationToken ct = default);

    /// Check if user has a cached token (async, connection-aware).
    public Task<bool> IsSignedInAsync(string? connectionName = null, CancellationToken ct = default);

    /// Check if user has a cached token (sync, backwards-compat, default connection).
    public bool IsSignedIn { get; }

    /// Get token status for all configured connections.
    public Task<IList<GetTokenStatusResult>> GetConnectionStatusAsync(CancellationToken ct = default);
}
```

### OAuthFlow Class

```csharp
public class OAuthFlow
{
    public string? ConnectionName { get; }

    public Task<string?> GetTokenAsync<TActivity>(Context<TActivity> context, CancellationToken ct = default);
    public Task<string?> SignInAsync<TActivity>(Context<TActivity> context, CancellationToken ct = default);
    public Task<string?> SignInAsync<TActivity>(Context<TActivity> context, OAuthOptions? options, CancellationToken ct = default);
    public Task SignOutAsync<TActivity>(Context<TActivity> context, CancellationToken ct = default);
    public Task<bool> IsSignedInAsync<TActivity>(Context<TActivity> context, CancellationToken ct = default);
    public Task<IList<GetTokenStatusResult>> GetConnectionStatusAsync<TActivity>(Context<TActivity> context, CancellationToken ct = default);

    public OAuthFlow OnSignInComplete(SignInCompleteHandler handler);
    public OAuthFlow OnSignInFailure(SignInFailureHandler handler);
}
```

### OAuthOptions

```csharp
public class OAuthOptions
{
    public string? ConnectionName { get; set; }
    public string OAuthCardText { get; set; } = "Please Sign In";
    public string SignInButtonText { get; set; } = "Sign In";
}
```

### Delegates

```csharp
public delegate Task SignInCompleteHandler(
    Context<TeamsActivity> context,
    GetTokenResult tokenResponse,
    CancellationToken cancellationToken);

public delegate Task SignInFailureHandler(
    Context<TeamsActivity> context,
    CancellationToken cancellationToken);
```

## Internal Flow

### SignInAsync Sequence

```
Developer calls context.SignIn(options) or oauth.SignInAsync(context)
    │
    ├─ 1. Check if message text is a magic code (4-8 digits)
    │     ├─ Yes → call GetTokenAsync(code) → return token if redeemed
    │     └─ No ↓
    │
    ├─ 2. Call UserTokenClient.GetTokenAsync(userId, connectionName, channelId)
    │     ├─ Token exists → return token string
    │     └─ No token ↓
    │
    ├─ 3. Build token exchange state with MsAppId (from BotApplication.AppId)
    │     Call UserTokenClient.GetSignInResourceAsync(state)
    │     Returns: SignInLink, TokenExchangeResource, TokenPostResource
    │
    ├─ 4. Build OAuthCard attachment (serialized as JsonElement for AOT compat):
    │     {
    │       contentType: "application/vnd.microsoft.card.oauth",
    │       content: {
    │         text: options.OAuthCardText,
    │         buttons: [{ type: "signin", title: options.SignInButtonText, value: signInLink }],
    │         connectionName: connectionName,
    │         tokenExchangeResource: { id, uri, providerId },
    │         tokenPostResource: { sasUrl }
    │       }
    │     }
    │
    ├─ 5. Send activity with OAuthCard attachment
    │
    └─ 6. Return null (sign-in pending)
```

**Critical**: The state must include `MsAppId` (from `BotApplication.AppId`, sourced from `BotConfig.ClientId`). Without it, the Token Service returns `tokenExchangeResource: null` and Teams cannot perform SSO or automatic verify-state after popup sign-in.

### signin/tokenExchange Invoke Handler

```
Teams client sends invoke: signin/tokenExchange
    │
    ├─ 1. Deserialize value → SignInTokenExchangeValue { Id, ConnectionName, Token }
    │
    ├─ 2. Deduplication check (by value.Id)
    │     ├─ Already processed → respond 200 (no-op)
    │     └─ New ↓
    │
    ├─ 3. Resolve OAuthFlow by ConnectionName
    │
    ├─ 4. Call UserTokenClient.ExchangeTokenAsync(userId, connectionName, channelId, token)
    │     ├─ Success → fire OnSignInComplete, respond InvokeResponse(200)
    │     └─ Failure → fire OnSignInFailure, respond InvokeResponse(412)
    │              (412 tells Teams to show the sign-in card as fallback)
    │
    └─ 5. Record exchange Id as processed (dedup)
```

### signin/verifyState Invoke Handler

```
Teams client sends invoke: signin/verifyState
    │
    ├─ 1. Deserialize value → SignInVerifyStateValue { State }
    │     (State is the code from the popup sign-in redirect)
    │
    ├─ 2. Try each registered OAuthFlow (verifyState has no connectionName):
    │     For each flow:
    │       Call UserTokenClient.GetTokenAsync(userId, connectionName, channelId, code: state)
    │       ├─ Token returned → fire OnSignInComplete, respond InvokeResponse(200), stop
    │       └─ No token → try next flow
    │
    ├─ 3. If no flow succeeded → respond InvokeResponse(400)
    │
    └─ Done
```

### Magic Code Message Handler

```
Message activity with 4-8 digit numeric text arrives
    │
    ├─ 1. Try each registered OAuthFlow:
    │     Call UserTokenClient.GetTokenAsync(userId, connectionName, channelId, code: text)
    │     ├─ Token returned → fire OnSignInComplete via HandleMagicCodeRedeemAsync, stop
    │     └─ No token → try next flow
    │
    └─ 2. If no flow redeemed the code → message continues to other handlers
```

### Deduplication

Teams may send duplicate `signin/tokenExchange` invokes because the user can have multiple active endpoints (mobile, desktop, web) and Teams sends the exchange request from each one. The `OAuthFlow` deduplicates by tracking processed exchange IDs.

**Default implementation**: In-process `ConcurrentDictionary<string, DateTimeOffset>` with a 5-minute TTL. This works for single-instance deployments and development.

**Production consideration**: When the bot is deployed behind a load balancer with multiple instances (e.g., Azure App Service scaled to N nodes), duplicate `signin/tokenExchange` invokes may arrive at **different instances**. The in-process cache cannot deduplicate across instances, so the token exchange may be attempted multiple times. While the Token Service is idempotent (duplicate exchanges succeed harmlessly), the `OnSignInComplete` callback may fire more than once.

For production multi-instance deployments, the deduplication store should be replaced with a distributed cache (e.g., Redis, Azure Cache). This is a future extensibility point -- the `OAuthFlow` should accept an `IDistributedCache` or similar abstraction to allow external storage:

```csharp
// Future API (not yet implemented)
bot.AddOAuthFlow("GraphConnection", options =>
{
    options.DeduplicationStore = new RedisDeduplicationStore(redisConnection);
});
```

Until this is implemented, multi-instance deployments should be aware that `OnSignInComplete` may fire on more than one instance for the same sign-in. Handlers should be idempotent.

### Auto-Discovery (no connection name)

When `AddOAuthFlow()` is called without a connection name:

1. On first call to `SignInAsync` / `GetTokenAsync` / `IsSignedInAsync`, calls `UserTokenClient.GetTokenStatusAsync(userId, channelId)`.
2. `GetTokenStatus` returns **all** configured OAuth connections on the bot (regardless of whether the user has a token).
3. If exactly one connection exists, uses it automatically.
4. If multiple connections exist, throws `InvalidOperationException` with a message listing the available connections and asking the developer to specify one.
5. The resolved connection name is cached for subsequent calls.

## Multi-Connection Sample

A bot that uses **two** OAuth connections: one for Microsoft Graph and one for GitHub.

### Configuration

Azure Bot resource has two OAuth connection settings:

| Connection name | Provider | Scopes |
|---|---|---|
| `GraphConnection` | Azure AD v2 | `User.Read Calendars.Read` |
| `GitHubConnection` | GitHub | `repo read:user` |

### Registration (using context API)

```csharp
var builder = WebApplication.CreateSlimBuilder(args);
builder.Services.AddTeamsBotApplication();
var app = builder.Build();
TeamsBotApplication bot = app.UseTeamsBotApplication();

// Register two OAuthFlow instances
OAuthFlow graphAuth = bot.AddOAuthFlow("GraphConnection");
OAuthFlow githubAuth = bot.AddOAuthFlow("GitHubConnection");

// Sign-in complete callbacks
graphAuth.OnSignInComplete(async (context, tokenResponse, ct) =>
{
    await context.SendActivityAsync($"Connected to Graph ({tokenResponse.ConnectionName})!", ct);
});

githubAuth.OnSignInComplete(async (context, tokenResponse, ct) =>
{
    await context.SendActivityAsync($"Connected to GitHub ({tokenResponse.ConnectionName})!", ct);
});

// Context-based API -- connection specified per-call
bot.OnMessage(@"(?i)^login graph$", async (context, ct) =>
{
    string? token = await context.SignIn(new OAuthOptions
    {
        ConnectionName = "GraphConnection",
        OAuthCardText = "Sign in to your Microsoft account",
        SignInButtonText = "Sign In to Graph"
    }, ct);

    if (token is not null)
        await context.SendActivityAsync("Already signed in to Graph.", ct);
});

bot.OnMessage(@"(?i)^login github$", async (context, ct) =>
{
    string? token = await context.SignIn(new OAuthOptions
    {
        ConnectionName = "GitHubConnection",
        OAuthCardText = "Sign in to your GitHub account",
        SignInButtonText = "Sign In to GitHub"
    }, ct);

    if (token is not null)
        await context.SendActivityAsync("Already signed in to GitHub.", ct);
});

bot.OnMessage(@"(?i)^status$", async (context, ct) =>
{
    var statuses = await context.GetConnectionStatusAsync(ct);
    var lines = statuses.Select(s =>
        $"- **{s.ConnectionName}** ({s.ServiceProviderDisplayName}): " +
        $"{(s.HasToken == true ? "connected" : "not connected")}");

    await context.SendActivityAsync("OAuth connections:\n" + string.Join("\n", lines), ct);
});

bot.OnMessage(@"(?i)^logout$", async (context, ct) =>
{
    await context.SignOut("GraphConnection", ct);
    await context.SignOut("GitHubConnection", ct);
    await context.SendActivityAsync("Signed out from all services.", ct);
});

app.Run();
```

### How Multi-Connection Invoke Routing Works

When multiple `OAuthFlow` instances are registered, invoke routes are registered **once** (shared). The dispatch logic differs by invoke type:

- **`signin/tokenExchange`**: dispatches by `connectionName` from the invoke value (exact match).
- **`signin/verifyState`**: tries each registered flow sequentially (no connection name in the payload).
- **`message/oauth/magicCode`**: tries each registered flow sequentially (magic code has no connection context).

## File Placement

| File | Location |
|---|---|
| `OAuthFlow.cs` | `Microsoft.Teams.Bot.Apps/Auth/OAuthFlow.cs` |
| `OAuthFlowExtensions.cs` | `Microsoft.Teams.Bot.Apps/Auth/OAuthFlowExtensions.cs` |
| `OAuthOptions.cs` | `Microsoft.Teams.Bot.Apps/Auth/OAuthOptions.cs` |
| `SignInTokenExchangeValue.cs` | `Microsoft.Teams.Bot.Apps/Auth/SignInTokenExchangeValue.cs` |
| `SignInVerifyStateValue.cs` | `Microsoft.Teams.Bot.Apps/Auth/SignInVerifyStateValue.cs` |
| `OAuthCard.cs` | `Microsoft.Teams.Bot.Apps/Schema/OAuthCard.cs` |

## Changes to Core

| File | Change |
|---|---|
| `BotApplication.cs` | Added `AppId` public property (from `BotApplicationOptions.AppId`) |
| `MessageHandler.cs` | Selectors now match against `TextWithoutMentions` instead of `Text` |
| `MessageActivity.cs` | Added `TextWithoutMentions` computed property (strips bot @mention) |
| `TeamsAttachment.cs` | Added `AttachmentContentType.OAuthCard` constant |

## Edge Cases & Constraints

| Scenario | Behavior |
|---|---|
| SSO not supported (channel scope) | SSO only works in personal and group chat. In channels, the OAuthCard shows the sign-in button directly (no token exchange). |
| User denies consent | Teams sends `signin/tokenExchange` but exchange fails. OAuthFlow responds 412, Teams shows sign-in button fallback. `OnSignInFailure` fires. |
| Duplicate `signin/tokenExchange` | Deduplicated by exchange ID. First wins, duplicates get 200 no-op. |
| Token expired | `GetTokenAsync` returns null (token store returns 404). `SignInAsync` re-initiates the flow. |
| Auto-discovery with multiple connections | Throws `InvalidOperationException` listing available connections. |
| `signin/verifyState` with multiple connections | Tries each registered flow until one succeeds (200). Returns 400 if none match. |
| `IsSignedIn` with multiple connections | Checks the first registered connection, logs `Trace.TraceWarning`. Prefer `IsSignedInAsync(connectionName)`. |
| Magic code in message | Intercepted by `message/oauth/magicCode` route. Tries each flow. If none redeem it, the message continues to other handlers. |
| Missing `MsAppId` in sign-in state | Token Service returns `tokenExchangeResource: null`. SSO and automatic verify-state fail. OAuthFlow includes `MsAppId` from `BotApplication.AppId` to prevent this. |
| Non-AAD providers (GitHub, etc.) | No `tokenExchangeResource` returned regardless of `MsAppId`. Sign-in completes via popup + `signin/verifyState` or magic code. |
| OAuthCard JSON serialization | `OAuthCard` is serialized to `JsonElement` before attaching, to avoid `NotSupportedException` from the source-generated `TeamsActivityJsonContext`. |
