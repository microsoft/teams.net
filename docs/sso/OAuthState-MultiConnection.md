# Pending OAuth Connections in Multi-Connection Flows

When a bot supports more than one OAuth connection, the callback payload does not always say which connection it belongs to. That is why the repo's pending-sign-in state matters.

The main problem is with `signin/failure` and `signin/verifyState`:

- `signin/tokenExchange` includes a `connectionName`
- `signin/failure` does **not**
- `signin/verifyState` does **not**

Without a connection name, the app has to guess which flow the callback belongs to. In a multi-connection bot, that is fragile.

## Example

Imagine two connections:

- `graph`
- `github`

The bot can start both flows:

```csharp
OAuthFlow graph = bot.GetOAuthFlow("graph");
OAuthFlow github = bot.GetOAuthFlow("github");

string? graphToken = await graph.SignInAsync(context, ct);
string? githubToken = await github.SignInAsync(context, ct);
```

Now the bot receives a callback like this:

```csharp
// signin/failure payload
new SignInFailureValue
{
    Code = "resource_match_failed",
    Message = "SSO could not complete"
}
```

There is no `connectionName` in that payload, so the app cannot tell whether it was `graph` or `github`.

## What state is doing here

We use turn state to remember which OAuth connections already started a sign-in.
That lets the app make a best-effort choice when Teams sends back a callback without a
connection name.

The flow stores per-connection timestamps in user state:

```csharp
string pendingKey = $"__oauth:pending:{_connectionName}";
string ssoPendingKey = $"__oauth:pending:sso:{_connectionName}";

context.State.UserState.Set(pendingKey, now);
if (ssoOffered)
{
    context.State.UserState.Set(ssoPendingKey, now);
}
```

If state is not available, it falls back to in-memory tracking:

```csharp
_pendingSignIns[userId] = now;
if (ssoOffered)
{
    _pendingSsoSignIns[userId] = now;
}
```

## Current behavior

The current implementation already has to work around the missing connection name:

```csharp
// signin/failure has no connection name, so the registry tries to attribute it
// to the flow with a pending silent-SSO sign-in from repo state.
OAuthFlow? target = registry.ResolvePendingSsoFlow(ctx);
```

That is only a best guess: it picks the most recent flow that offered silent SSO, and if
that cannot be resolved it falls back to notifying every flow.

For `signin/verifyState`, the registry first tries the most recently pending flow, then falls back to the rest:

```csharp
// Pseudocode: pick the most recent pending flow, then try the others.
OAuthFlow? mostRecent = ...;
if (mostRecent is not null)
{
    var response = await mostRecent.HandleVerifyStateAsync(ctx, verifyValue, ct);
    if (response.Status == 200)
    {
        return response;
    }
}

foreach (OAuthFlow flow in registry.GetAllFlows())
{
    if (flow == mostRecent)
    {
        continue;
    }

    var response = await flow.HandleVerifyStateAsync(ctx, verifyValue, ct);
    if (response.Status == 200)
    {
        return response;
    }
}
```

That works, but it is still a fallback.

## What would be better

The service should include the `connectionName` in `signin/failure` and `signin/verifyState`, just like it already does for token exchange.

```csharp
// Ideal payload shape
new
{
    connectionName = "graph",
    state = "flow-123"
}
```

If that were available, the app would not need to track the most recent sign-in failure or search across all flows.

## Bottom line

In multi-connection OAuth, repo state is a stop gap for remembering recent pending connections. It keeps the callbacks working, but this flow is not ideal and really needs service changes so `signin/failure` and `signin/verifyState` carry enough context to identify the right connection directly.
