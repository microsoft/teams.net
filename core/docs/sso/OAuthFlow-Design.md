# OAuthFlow Design

## What it is

`OAuthFlow` is a high-level helper for adding user sign-in (OAuth / Teams SSO) to a bot.
It wraps the whole lifecycle — silent token lookup, SSO token exchange, interactive
fallback sign-in, and sign-out — so you don't have to wire up the Bot Framework token
protocol by hand.

It builds on `UserTokenClient` and auto-registers the invoke handlers the sign-in
protocol requires. You register one flow per OAuth connection and attach callbacks.

## Quick start

```csharp
// Register a flow (one per OAuth connection configured on your Azure Bot resource)
builder.Services.AddTeamsBotApplication(o => o.AddOAuthFlow("graph"));

var bot = app.UseTeamsBotApplication();

bot.GetOAuthFlow("graph")
   .OnSignInComplete((ctx, token, ct) => ctx.SendActivityAsync("Signed in!", ct))
   .OnSignInFailure((ctx, failure, ct) => ctx.SendActivityAsync("Sign-in failed.", ct));

// Trigger sign-in for the current user
bot.OnMessage(@"(?i)^login$", async (ctx, ct) =>
{
    string? token = await bot.GetOAuthFlow("graph").SignInAsync(ctx, ct);
    if (token is not null) await ctx.SendActivityAsync("Already signed in.", ct);
});
```

`SignInAsync` returns the cached token if one exists, or `null` after sending an
OAuth card to start the sign-in.

## How sign-in works

`SignInAsync` first asks the token store for an existing token. If none exists, it fetches
a sign-in resource and sends an **OAuth card**. Teams then drives the rest through three
invoke activities, all handled for you:

| Invoke | Meaning |
| --- | --- |
| `signin/tokenExchange` | Silent **SSO** — Teams exchanges an Entra token for a connection token. Carries a connection name; deduplicated. On success fires `OnSignInComplete`. |
| `signin/verifyState` | **Interactive** completion — the user clicked the sign-in button and Teams returns a single-use code to redeem. Used by non-SSO connections *and* by SSO connections that fell back to the button. |
| `signin/failure` | Client-side **SSO** failure (e.g. `resourcematchfailed`). Fires `OnSignInFailure`. |

> The sign-in state must include the bot's `MsAppId` (from `BotApplication.AppId`).
> Without it the token service returns no `TokenExchangeResource` and silent SSO can't run.

## Multiple connections

You can register several flows (e.g. `graph` and `github`). Invoke routes are registered
once and shared; dispatch differs per invoke:

- **`signin/tokenExchange`** carries a connection name → resolved exactly.
- **`signin/verifyState`** has no connection name → each flow is tried until one redeems the code.
- **`signin/failure`** has no connection name. Since Teams only emits it for silent SSO,
  it's attributed to the flow with the most recent pending **SSO** sign-in
  (`OAuthFlowRegistry.ResolvePendingSsoFlow`). This avoids firing the failure callback on a
  non-SSO connection (like GitHub) that merely signed in more recently. If none resolves,
  all flows are notified rather than dropping the callback.

## Deduplication

Teams sends `signin/tokenExchange` from every active client endpoint (desktop, web,
mobile), so the same exchange can arrive several times. Without dedup, `OnSignInComplete`
(and its side effects) would fire multiple times.

`OAuthFlow` deduplicates by exchange ID: an in-process `ConcurrentDictionary` (atomic,
5-minute TTL) plus a conversation-state marker for cross-instance coordination when
`UseState()` is configured. Duplicates get a `200` no-op.

`signin/verifyState` and `signin/failure` are **not** deduplicated — the verify code is
single-use (naturally idempotent) and failure is a single informational notice.

## Reserved state keys

Internal keys use a `__` prefix; don't read or write them from app code.

| Key | Scope | Purpose |
| --- | --- | --- |
| `__oauth:exchange:{id}` | Conversation | Cross-instance dedup for token exchange |
| `__oauth:pending:{conn}` | User | A sign-in is in progress for this connection |
| `__oauth:pending:sso:{conn}` | User | The pending sign-in offered silent SSO (used to attribute `signin/failure`) |

## API surface

```csharp
public class OAuthFlow
{
    public string ConnectionName { get; }

    public Task<string?> SignInAsync<T>(Context<T> ctx, CancellationToken ct = default);
    public Task<string?> GetTokenAsync<T>(Context<T> ctx, CancellationToken ct = default);   // silent-only
    public Task SignOutAsync<T>(Context<T> ctx, CancellationToken ct = default);
    public Task<bool> IsSignedInAsync<T>(Context<T> ctx, CancellationToken ct = default);
    public Task<IList<GetTokenStatusResult>> GetConnectionStatusAsync<T>(Context<T> ctx, CancellationToken ct = default);

    public OAuthFlow OnSignInComplete(SignInCompleteHandler handler);
    public OAuthFlow OnSignInFailure(SignInFailureHandler handler);
}

public class OAuthOptions
{
    public string? ConnectionName { get; set; }
    public string OAuthCardText { get; set; } = "Please Sign In";
    public string SignInButtonText { get; set; } = "Sign In";
}

public delegate Task SignInCompleteHandler(Context<TeamsActivity> ctx, GetTokenResult token, CancellationToken ct);
public delegate Task SignInFailureHandler(Context<TeamsActivity> ctx, SignInFailureValue? failure, CancellationToken ct);
```

`failure` is non-null for `signin/failure` invokes (structured `Code`/`Message` from the
Teams client) and null for server-side token-exchange / verify-state failures.

## Edge cases

| Scenario | Behavior |
| --- | --- |
| Channel or group chat | Silent SSO can't complete, so the card omits `TokenExchangeResource` and Teams shows the sign-in button directly. |
| `resourcematchfailed` | The silent SSO leg failed — the Entra app's *Expose an API → Application ID URI* must match the connection's Token Exchange URL (`api://<domain>/botid-<appId>`). Teams then falls back to the button. |
| User denies consent / exchange fails | Responds `412`, Teams shows the button fallback; `OnSignInFailure` fires with `failure: null`. |
| Token expired | `GetTokenAsync` returns null; `SignInAsync` restarts the flow. |
| Non-Entra provider (GitHub, etc.) | No `TokenExchangeResource`; sign-in always completes via the button + `signin/verifyState`. |
| Unknown connection name | `GetOAuthFlow`/`SignIn` throw, listing the registered connections. |
| No name given + multiple flows (obsolete `context.SignIn()`) | Throws asking you to specify a connection name. The recommended `OAuthFlow` API always names the connection, so it never hits this. |

