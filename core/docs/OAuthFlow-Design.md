# OAuthFlow Design Document

## Overview

`OAuthFlow` provides a high-level abstraction for Teams Bot SSO (Single Sign-On) authentication. It encapsulates the full OAuth lifecycle -- silent token acquisition, SSO token exchange, fallback sign-in, and sign-out -- so developers can add user authentication with minimal plumbing.

The design builds on top of the existing `UserTokenClient` (core) and `UserTokenApiClient` / `BotSignInClient` (Apps layer), and follows the handler-based routing pattern established by `AdaptiveCardExtensions`, `TaskExtensions`, etc.

## Motivation

Teams SSO requires coordinating multiple moving parts:

1. Checking the Bot Framework Token Store for an existing token
2. Sending an OAuthCard with a `TokenExchangeResource` to trigger silent SSO
3. Handling `signin/tokenExchange` invoke activities (with deduplication)
4. Handling `signin/verifyState` invoke activities (fallback magic-code flow)
5. Calling `UserTokenClient.ExchangeTokenAsync` to complete the on-behalf-of exchange

Without an abstraction, every bot developer must wire this up manually. `OAuthFlow` reduces it to a few method calls.

## Architecture

```
TeamsBotApplication
├── Router
│   ├── ... existing routes ...
│   ├── invoke/signin/tokenExchange   ← registered by OAuthFlow
│   └── invoke/signin/verifyState     ← registered by OAuthFlow
└── OAuthFlow (one per connection)
    ├── SignInAsync()        → silent token check + OAuthCard
    ├── SignOutAsync()       → revoke token
    ├── IsSignedInAsync()    → check token store
    ├── GetTokenAsync()      → silent-only token retrieval
    ├── OnSignInComplete()   → callback after successful exchange
    └── OnSignInFailure()    → callback on exchange failure
```

### Relationship to existing clients

```
OAuthFlow (Apps layer - developer-facing)
    │
    ├── UserTokenApiClient.GetAsync()          → silent token check
    ├── UserTokenApiClient.ExchangeAsync()     → SSO token exchange
    ├── UserTokenApiClient.GetStatusAsync()    → connection discovery & status
    ├── UserTokenApiClient.SignOutAsync()       → sign-out
    └── BotSignInClient.GetResourceAsync()     → sign-in resource (OAuthCard data)
```

`OAuthFlow` does **not** replace these clients. It orchestrates them into a cohesive flow and auto-registers the invoke handlers that the SSO protocol requires.

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

`AddOAuthFlow` registers two invoke routes on the app's `Router`:

| Route name | Invoke name | Purpose |
|---|---|---|
| `invoke/signin/tokenExchange` | `signin/tokenExchange` | SSO silent token exchange |
| `invoke/signin/verifyState` | `signin/verifyState` | Fallback magic-code verification |

When multiple `OAuthFlow` instances are registered (multi-connection), the invoke handlers dispatch to the correct flow by matching the `connectionName` in the invoke value.

### OAuthFlow Class

```csharp
public class OAuthFlow
{
    /// The OAuth connection name. Null until resolved (auto-discovery mode).
    public string? ConnectionName { get; }

    /// Attempt silent token acquisition from the token store.
    /// Returns the access token string, or null if no token is cached.
    public Task<string?> GetTokenAsync<TActivity>(
        Context<TActivity> context,
        CancellationToken cancellationToken = default) where TActivity : TeamsActivity;

    /// Attempt silent token acquisition; if no token is available,
    /// send an OAuthCard to initiate the SSO flow.
    /// Returns the token if already cached, or null if SSO was initiated
    /// (the result will arrive via OnSignInComplete).
    public Task<string?> SignInAsync<TActivity>(
        Context<TActivity> context,
        CancellationToken cancellationToken = default) where TActivity : TeamsActivity;

    /// Sign the user out, revoking their token from the token store.
    public Task SignOutAsync<TActivity>(
        Context<TActivity> context,
        CancellationToken cancellationToken = default) where TActivity : TeamsActivity;

    /// Check whether the user has a valid cached token.
    public Task<bool> IsSignedInAsync<TActivity>(
        Context<TActivity> context,
        CancellationToken cancellationToken = default) where TActivity : TeamsActivity;

    /// Get the token status for all configured OAuth connections.
    /// This calls GetTokenStatus which returns every connection
    /// registered on the bot, so the developer never needs to
    /// enumerate connection names manually.
    public Task<IList<GetTokenStatusResult>> GetConnectionStatusAsync<TActivity>(
        Context<TActivity> context,
        CancellationToken cancellationToken = default) where TActivity : TeamsActivity;

    /// Register a callback invoked after a successful token exchange
    /// (SSO or fallback sign-in).
    public OAuthFlow OnSignInComplete(SignInCompleteHandler handler);

    /// Register a callback invoked when token exchange fails.
    public OAuthFlow OnSignInFailure(SignInFailureHandler handler);
}
```

### Delegates

```csharp
public delegate Task SignInCompleteHandler(
    Context<InvokeActivity> context,
    GetTokenResult tokenResponse,
    CancellationToken cancellationToken);

public delegate Task SignInFailureHandler(
    Context<InvokeActivity> context,
    CancellationToken cancellationToken);
```

### Value Types

```csharp
/// Value payload of the signin/tokenExchange invoke activity.
public class SignInTokenExchangeValue
{
    public string? Id { get; set; }
    public string? ConnectionName { get; set; }
    public string? Token { get; set; }
}

/// Value payload of the signin/verifyState invoke activity.
public class SignInVerifyStateValue
{
    public string? State { get; set; }
}
```

## Internal Flow

### SignInAsync Sequence

```
Developer calls oauth.SignInAsync(context)
    │
    ├─ 1. Call UserTokenClient.GetTokenAsync(userId, connectionName, channelId)
    │     ├─ Token exists → return token string
    │     └─ No token ↓
    │
    ├─ 2. Call UserTokenClient.GetSignInResource(userId, connectionName, channelId)
    │     Returns: SignInLink, TokenExchangeResource, TokenPostResource
    │
    ├─ 3. Build OAuthCard attachment:
    │     {
    │       contentType: "application/vnd.microsoft.card.oauth",
    │       content: {
    │         buttons: [{ type: "signin", title: "Sign In", value: signInLink }],
    │         connectionName: connectionName,
    │         tokenExchangeResource: { id, uri, providerId },
    │         tokenPostResource: { sasUrl }
    │       }
    │     }
    │
    ├─ 4. Send activity with OAuthCard attachment
    │
    └─ 5. Return null (SSO exchange pending)
```

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
    │     (State contains the magic code from fallback sign-in)
    │
    ├─ 2. Call UserTokenClient.GetTokenAsync(userId, connectionName, channelId, code: state)
    │     ├─ Token returned → fire OnSignInComplete, respond InvokeResponse(200)
    │     └─ No token → fire OnSignInFailure, respond InvokeResponse(400)
    │
    └─ Done
```

### Deduplication

Teams may send duplicate `signin/tokenExchange` invokes (the user can have multiple active endpoints -- mobile, desktop, web). The `OAuthFlow` deduplicates by tracking processed exchange IDs in a `ConcurrentDictionary<string, byte>` with a short TTL. This is an in-process, per-instance cache -- sufficient because duplicates arrive within milliseconds of each other to the same bot instance.

### Auto-Discovery (no connection name)

When `AddOAuthFlow()` is called without a connection name:

1. On first call to `SignInAsync` / `GetTokenAsync` / `IsSignedInAsync`, calls `UserTokenClient.GetTokenStatusAsync(userId, channelId)`.
2. `GetTokenStatus` returns **all** configured OAuth connections on the bot (regardless of whether the user has a token).
3. If exactly one connection exists, uses it automatically.
4. If multiple connections exist, throws `InvalidOperationException` with a message listing the available connections and asking the developer to specify one.
5. The resolved connection name is cached for subsequent calls.

This eliminates the need for developers to hard-code connection names when only one connection is configured, which is the common case.

## Multi-Connection Sample

A bot that uses **two** OAuth connections: one for Microsoft Graph (user profile, calendar) and one for a third-party API (e.g., Salesforce).

### Configuration

Azure Bot resource has two OAuth connection settings:

| Connection name | Provider | Scopes |
|---|---|---|
| `GraphConnection` | Azure AD v2 | `User.Read Calendars.Read` |
| `GitHubConnection` | GitHub | `repo read:user` |

### Registration

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTeams("AzureAd");

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

var bot = app.UseTeams("api/messages");

// Register two OAuthFlow instances, one per connection
OAuthFlow graphAuth = bot.AddOAuthFlow("GraphConnection");
OAuthFlow githubAuth = bot.AddOAuthFlow("GitHubConnection");

// --- Sign-in complete callbacks ---

graphAuth.OnSignInComplete(async (context, tokenResponse, ct) =>
{
    await context.SendActivityAsync("Connected to Microsoft Graph!", ct);
});

githubAuth.OnSignInComplete(async (context, tokenResponse, ct) =>
{
    await context.SendActivityAsync("Connected to GitHub!", ct);
});

// --- Message handlers ---

bot.OnMessage(@"^login graph$", async (context, ct) =>
{
    string? token = await graphAuth.SignInAsync(context, ct);
    if (token != null)
    {
        await context.SendActivityAsync("Already signed in to Graph.", ct);
    }
    // else: OAuthCard sent, SSO in progress
});

bot.OnMessage(@"^login github$", async (context, ct) =>
{
    string? token = await githubAuth.SignInAsync(context, ct);
    if (token != null)
    {
        await context.SendActivityAsync("Already signed in to GitHub.", ct);
    }
});

bot.OnMessage(@"^status$", async (context, ct) =>
{
    // GetConnectionStatusAsync returns ALL connections -- no names needed
    var statuses = await graphAuth.GetConnectionStatusAsync(context, ct);
    var lines = statuses.Select(s =>
        $"- **{s.ConnectionName}** ({s.ServiceProviderDisplayName}): " +
        $"{(s.HasToken == true ? "connected" : "not connected")}");

    await context.SendActivityAsync(
        "OAuth connections:\n" + string.Join("\n", lines), ct);
});

bot.OnMessage(@"^my calendar$", async (context, ct) =>
{
    string? token = await graphAuth.SignInAsync(context, ct);
    if (token == null) return;

    // Call Graph API with the token
    using var http = new HttpClient();
    http.DefaultRequestHeaders.Authorization = new("Bearer", token);
    var response = await http.GetStringAsync(
        "https://graph.microsoft.com/v1.0/me/events?$top=3", ct);

    await context.SendActivityAsync($"Your next events:\n{response}", ct);
});

bot.OnMessage(@"^my repos$", async (context, ct) =>
{
    string? token = await githubAuth.SignInAsync(context, ct);
    if (token == null) return;

    // Call GitHub API with the token
    using var http = new HttpClient();
    http.DefaultRequestHeaders.Authorization = new("Bearer", token);
    http.DefaultRequestHeaders.UserAgent.ParseAdd("TeamsBot/1.0");
    var response = await http.GetStringAsync(
        "https://api.github.com/user/repos?sort=updated&per_page=5", ct);

    await context.SendActivityAsync($"Your recent repos:\n{response}", ct);
});

bot.OnMessage(@"^logout$", async (context, ct) =>
{
    // Sign out from both connections
    await graphAuth.SignOutAsync(context, ct);
    await githubAuth.SignOutAsync(context, ct);
    await context.SendActivityAsync("Signed out from all services.", ct);
});

bot.OnMessage(@"^logout graph$", async (context, ct) =>
{
    await graphAuth.SignOutAsync(context, ct);
    await context.SendActivityAsync("Signed out from Graph.", ct);
});

bot.OnMessage(@"^logout github$", async (context, ct) =>
{
    await githubAuth.SignOutAsync(context, ct);
    await context.SendActivityAsync("Signed out from GitHub.", ct);
});

app.Run();
```

### How Multi-Connection Invoke Routing Works

When multiple `OAuthFlow` instances are registered, both `signin/tokenExchange` and `signin/verifyState` invoke routes are registered **once** (shared). The shared handler dispatches to the correct `OAuthFlow` instance by matching `connectionName` from the invoke value:

```
signin/tokenExchange invoke arrives
    │
    ├─ value.ConnectionName == "GraphConnection"
    │   → dispatch to graphAuth
    │
    └─ value.ConnectionName == "GitHubConnection"
        → dispatch to githubAuth
```

This is handled internally by a registry (`Dictionary<string, OAuthFlow>`) keyed by connection name.

## Single-Connection Sample (Auto-Discovery)

When only one OAuth connection is configured, the developer can omit the connection name entirely:

```csharp
var bot = app.UseTeams("api/messages");

// No connection name -- auto-discovered via GetTokenStatus
OAuthFlow auth = bot.AddOAuthFlow();

auth.OnSignInComplete(async (context, tokenResponse, ct) =>
{
    await context.SendActivityAsync($"Signed in via {tokenResponse.ConnectionName}!", ct);
});

bot.OnMessage(@"^login$", async (context, ct) =>
{
    string? token = await auth.SignInAsync(context, ct);
    if (token != null)
    {
        await context.SendActivityAsync("You're already signed in.", ct);
    }
});

bot.OnMessage(@"^logout$", async (context, ct) =>
{
    await auth.SignOutAsync(context, ct);
    await context.SendActivityAsync("Signed out.", ct);
});

bot.OnMessage(@"^whoami$", async (context, ct) =>
{
    string? token = await auth.SignInAsync(context, ct);
    if (token == null) return;

    using var http = new HttpClient();
    http.DefaultRequestHeaders.Authorization = new("Bearer", token);
    var me = await http.GetStringAsync("https://graph.microsoft.com/v1.0/me", ct);
    await context.SendActivityAsync(me, ct);
});

app.Run();
```

## File Placement

| File | Location |
|---|---|
| `OAuthFlow.cs` | `Microsoft.Teams.Bot.Apps/Auth/OAuthFlow.cs` |
| `OAuthFlowExtensions.cs` | `Microsoft.Teams.Bot.Apps/Auth/OAuthFlowExtensions.cs` |
| `SignInTokenExchangeValue.cs` | `Microsoft.Teams.Bot.Apps/Auth/SignInTokenExchangeValue.cs` |
| `SignInVerifyStateValue.cs` | `Microsoft.Teams.Bot.Apps/Auth/SignInVerifyStateValue.cs` |
| `OAuthCard.cs` | `Microsoft.Teams.Bot.Apps/Schema/OAuthCard.cs` |

## Edge Cases & Constraints

| Scenario | Behavior |
|---|---|
| SSO not supported (channel scope) | SSO only works in personal and group chat. In channels, the OAuthCard shows the sign-in button directly (no token exchange). |
| User denies consent | Teams sends `signin/tokenExchange` but exchange fails. OAuthFlow responds 412, Teams shows sign-in button fallback. `OnSignInFailure` fires. |
| Duplicate `signin/tokenExchange` | Deduplicated by exchange ID. First wins, duplicates get 200 no-op. |
| Token expired | `GetTokenAsync` returns null (token store returns 404). `SignInAsync` re-initiates the flow. |
| Auto-discovery with multiple connections | Throws `InvalidOperationException` listing available connections. |
| `signin/verifyState` with invalid code | `GetTokenAsync` with code returns null. `OnSignInFailure` fires. Response 400. |
