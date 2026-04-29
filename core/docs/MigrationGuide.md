# Migration Guide: Libraries/Microsoft.Teams.Apps to core/src/Microsoft.Teams.Apps

This guide covers migrating from the old `Microsoft.Teams.Apps` library (`Libraries/`) to the new `Microsoft.Teams.Apps` library (`core/src/`).

---

## Quick Reference

| Old API | New API | Notes |
|---------|---------|-------|
| `builder.AddTeams()` | `builder.AddTeams()` | Now works on both `WebApplicationBuilder` and `IServiceCollection` |
| `context.Send("text", ct)` | `context.Send("text", ct)` | Same API |
| `context.Send(activity, ct)` | `context.Send(activity, ct)` | Same API |
| `context.Reply("text", ct)` | `context.Reply("text", ct)` | Same API |
| `context.Reply(activity, ct)` | `context.Reply(activity, ct)` | Same API |
| `context.Typing("text", ct)` | `context.Typing("text", ct)` | Same API |
| `context.Log.Info(...)` | `context.Log.Info(...)` | Same API, delegates to `ILogger` |
| `context.Log.Error(...)` | `context.Log.Error(...)` | Same API |
| `context.Log.Debug(...)` | `context.Log.Debug(...)` | Same API |
| `context.Log.Warn(...)` | `context.Log.Warn(...)` | Same API |
| `context.AppId` | `context.AppId` | Same API |
| `teams.OnMeetingJoin(h)` | `teams.OnMeetingJoin(h)` | Alias for `OnMeetingParticipantJoin` |
| `teams.OnMeetingLeave(h)` | `teams.OnMeetingLeave(h)` | Alias for `OnMeetingParticipantLeave` |
| `teams.Send(convId, text)` | `teams.Send(convId, text)` | Proactive messaging |
| `teams.Reply(convId, msgId, text)` | `teams.Reply(convId, msgId, text)` | Proactive threaded reply |
| `InvokeResponse(200, body)` | `InvokeResponse.Ok(body)` | Factory method available |
| `InvokeResponse(400, body)` | `InvokeResponse.Error(400, body)` | Factory method available |

---

## Backward-Compatible Changes (No Migration Needed)

These APIs have been added to the new library to match the old API surface. Existing code using these patterns will work without changes.

### Context Convenience Methods (BC-1)

The following methods are available on `Context<TActivity>`:

```csharp
// Send a text message
await context.Send("Hello!", cancellationToken);

// Send an activity
await context.Send(myActivity, cancellationToken);

// Send a threaded reply
await context.Reply("This is a reply", cancellationToken);
await context.Reply(myActivity, cancellationToken);

// Send typing indicator
await context.Typing(cancellationToken: cancellationToken);
```

> **Note:** `Send(AdaptiveCard)` and `Reply(AdaptiveCard)` are not yet available to avoid a dependency on `Microsoft.Teams.Cards`. Use `TeamsActivityBuilder` with `AddAdaptiveCardAttachment()` instead.

### Context Logger (BC-2)

`context.Log` provides `.Info()`, `.Error()`, `.Debug()`, and `.Warn()` methods:

```csharp
context.Log.Info("Processing message");
context.Log.Error("Something failed", ex.Message);
context.Log.Debug("Activity ID:", context.Activity.Id);
```

These delegate to `Microsoft.Extensions.Logging.ILogger` under the hood. The underlying `ILogger` is accessible via `context.Log.Logger` if needed.

### Context AppId (BC-5)

```csharp
var appId = context.AppId; // reads from TeamsBotApplication.AppId
```

### WebApplicationBuilder.AddTeams() (BC-7)

Both styles work:
```csharp
// Old style (on WebApplicationBuilder)
builder.AddTeams();

// New style (on IServiceCollection)
builder.Services.AddTeams();
```

### Meeting Handler Aliases (BC-10)

Both old and new names work:
```csharp
// Old names
teams.OnMeetingJoin(handler);
teams.OnMeetingLeave(handler);

// New names (preferred)
teams.OnMeetingParticipantJoin(handler);
teams.OnMeetingParticipantLeave(handler);
```

### Proactive Messaging (BC-8)

```csharp
// Send proactively to a conversation
await teams.Send(conversationId, "Hello!", cancellationToken: ct);

// Send a threaded reply proactively
await teams.Reply(conversationId, messageId, "Replying!", ct);
```

> **Note:** The service URL is automatically cached from incoming activities. If you need to send proactively before any activity has been received, pass a `serviceUrl` parameter to `Send()`.

### InvokeResponse Factory Methods (BC-12)

```csharp
// Instead of: new InvokeResponse(200, body)
return InvokeResponse.Ok(body);

// Typed version
return InvokeResponse.Ok<TaskModuleResponse>(response);

// Error responses
return InvokeResponse.Error(400, errorDetails);
```

---

### App.Builder() Pattern (BC-6)

`App.Builder()` is supported with `AddOAuth()`:

```csharp
// This works in both old and new libraries:
var appBuilder = App.Builder()
    .AddOAuth("graph");
builder.AddTeams(appBuilder);
```

The following `AppBuilder` methods from the old library are **not available** and should use standard ASP.NET DI instead:

| Old AppBuilder Method | New Equivalent |
|----------------------|----------------|
| `.AddLogger(new ConsoleLogger(...))` | `builder.Logging.AddConsole()` |
| `.AddStorage(storage)` | Register via `builder.Services.AddSingleton<IStorage>(...)` |
| `.AddClient(httpClient)` | Register via `builder.Services.AddHttpClient(...)` |
| `.AddCredentials(credentials)` | Configure in `appsettings.json` AzureAd section |
| `.AddPlugin(plugin)` | No equivalent — plugins are not supported in the new library |
| `.AddCloud(cloud)` | Configure via `appsettings.json` |

---

## Breaking Changes Requiring Migration

### BC-4: `context.Ref` Removed

**Old:**
```csharp
var conversationId = context.Ref.Conversation.Id;
```

**New:**
```csharp
var conversationId = context.Activity.Conversation.Id;
```

The `Ref` property is not available. Use `context.Activity.Conversation` directly — it contains the same data.

---

### BC-9: `OnSignIn` / `OnSignInFailure` Events

**Old:**
```csharp
teams.OnSignIn(async (_, @event, cancellationToken) => { ... });
teams.OnSignInFailure(async (context, cancellationToken) => { ... });
```

**New:**
```csharp
var flow = teams.GetOAuthFlow("graph");
flow.OnSignInComplete(async (context, token, cancellationToken) => { ... });
flow.OnSignInFailure(async (context, cancellationToken) => { ... });
```

Sign-in events are now per-flow callbacks, which is more flexible when using multiple OAuth connections.

---

### BC-13: Activity Namespace Changes

| Old Namespace | New Namespace |
|---------------|---------------|
| `Microsoft.Teams.Api.Activities` | `Microsoft.Teams.Apps.Schema` |
| `MessageActivity` | `MessageActivity` (same name) |
| `InvokeActivity` | `InvokeActivity` (same name) |
| `IActivity` | `TeamsActivity` (base class) |

Member access (`.Text`, `.From`, `.Conversation`, `.Value`, etc.) remains the same. Only `using` statements need updating.

---

## Items Under Review

The following items are being evaluated and may change:

- **BC-1 (partial):** `Send(AdaptiveCard)` / `Reply(AdaptiveCard)` — pending Teams.Cards dependency decision
- **BC-3:** Middleware / `OnActivity` / `Use()` / `Next()` — architecture review needed
- **BC-11:** `OnSetting()` message extension handler — activity type clarification needed
- **BC-14:** `AddTab()` — scope of feature TBD
