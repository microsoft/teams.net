# Reduce Breaking Changes: Libraries/Microsoft.Teams.Apps to core/src/Microsoft.Teams.Apps

This is the single source of truth for the breaking-change reduction effort between the old `Microsoft.Teams.Apps` library (`Libraries/`) and the new one (`core/src/`). It covers both the per-change status (what the new library implements) and the concrete migration steps for your app code.

---

## Status at a Glance

Legend:

- ✅ **Implemented** — the new library provides a compatible API; existing code works as-is (or with a trivial change).
- 📝 **Requires migration** — no pending library work; your code must change as described, whether because the behavior moved (doc-only) or the API was intentionally dropped (use the documented alternative).
- ⚠️ **Under review** — not yet available / pending an architecture decision.

| Item | Status | Summary | Samples affected |
|---|---|---|---|
| BC-1 | ✅ (partial) | `context.Send/Reply/Typing` convenience methods (`AdaptiveCard` overloads deferred) | Echo, Cards, Dialogs, Graph, Meetings, MessageExtensions, Reactions, TargetedMessages, Threading, Tab, Lights |
| BC-2 | ✅ (deprecated) | `context.Log` (`Info/Error/Debug/Warn`) shim — `[Obsolete]`, use a DI `ILogger` | 12 samples |
| BC-3 | ⚠️ | Middleware / `OnActivity` / `Use()` / `context.Next()` | Echo, Graph, Meetings, TargetedMessages |
| BC-4 | 📝 | `context.Ref` removed → use `context.Activity.Conversation` | Threading, TargetedMessages |
| BC-5 | ✅ | `context.AppId` | Echo |
| BC-6 | ✅ (deprecated) | `App.Builder().AddOAuth()` shim — `[Obsolete]`, use DI | Graph, Meetings |
| BC-7 | ✅ (deprecated) | `AddTeams()` / `UseTeams()` shims — `[Obsolete]`, use `AddTeamsBotApplication()` / `UseTeamsBotApplication()` | All |
| BC-8 | ✅ | Proactive `teams.Send()` / `teams.Reply()` | Threading |
| BC-9 | 📝 | `OnSignIn` / `OnSignInFailure` → per-flow `OAuthFlow` callbacks | Graph |
| BC-11 | ⚠️ | `OnSetting()` message-extension handler | MessageExtensions |
| BC-12 | ✅ | `InvokeResponse.Ok()` / `.Error()` factory methods | Cards, Dialogs, MessageExtensions, Lights |
| BC-13 | 📝 | Activity namespace `Microsoft.Teams.Api.Activities` → `Microsoft.Teams.Apps.Schema` | type usage |
| BC-14 | ⚠️ | `app.AddTab()` | Dialogs, Tab |
| BC-15 | ✅ | `MessageActivity` fluent methods | — |
| BC-17 | ✅ (mostly) | Base activity `With*()` methods as extensions (`WithRelatesTo` excluded) | — |
| BC-18 | 📝 | `activity.ToMessage()` → `MessageActivity.FromActivity()` | — |
| BC-19 | ⚠️ | Missing activity types (`TypingActivity`, etc.) | — |
| BC-20 | ⚠️ | Missing handler registration methods (Tab, Command, infra) | — |
| BC-21 | 📝 | Type incompatibilities (`Timestamp`, `ServiceUrl`, enums, `Account`) | — |
| BC-22 | ✅ | `Conversation.ToThreadedConversationId()` (moved namespace) | — |
| BC-23 | ⚠️ | `MessageActivity` commented-out properties (`Summary`, `DeliveryMode`, etc.) | — |
| BC-24 | 📝 | `SuggestedActions` fluent methods removed | — |

### Sample migration readiness

"Remaining blockers" lists only pending library-side work. Implemented items are no longer blockers.

| Sample | Difficulty | Remaining blockers |
|--------|-----------|-------------|
| Samples.Cards | **Ready** | None |
| Samples.Reactions | **Ready** | None |
| Samples.Threading | **Ready** | BC-4 (doc-only: `context.Ref` → `context.Activity.Conversation`) |
| Samples.Echo | **Easy** | BC-3 (OnActivity/Next — pending) |
| Samples.TargetedMessages | **Easy** | BC-3 (OnActivity — pending) |
| Samples.Meetings | **Easy** | BC-3 (Use/Next — pending) |
| Samples.Dialogs | **Easy** | BC-14 (AddTab — pending) |
| Samples.MessageExtensions | **Easy** | BC-20 (OnSetting — pending) |
| Samples.Graph | **Easy-Med** | BC-3 (Use/Next — pending), BC-9 (SignIn events — doc-only) |
| Samples.BotBuilder | **Hard** | Entire `AddBotBuilder<>()` pattern missing |
| Deprecated.Controllers | **N/A** | Already deprecated, no migration needed |

---

## Assembly Mapping

The old library is split across 16 assemblies. The new library consolidates into 3.

### New library assemblies

| New Assembly | Purpose |
|---|---|
| `Microsoft.Teams.Core` | Foundation: activity protocol, auth, middleware, HTTP clients |
| `Microsoft.Teams.Apps` | High-level: handlers, routing, OAuth flows, API clients |
| `Microsoft.Teams.Apps.BotBuilder` | Backward compat layer for Bot Framework SDK |

### Old assemblies not available in the new library

These assemblies have no equivalent in the new library and must be sourced separately or replaced:

| Old Assembly | Status |
|---|---|
| `Microsoft.Teams.AI` | Not available |
| `Microsoft.Teams.AI.Models.OpenAI` | Not available |
| `Microsoft.Teams.Cards` | Not available |
| `Microsoft.Teams.Extensions.Graph` | Not available |
| `Microsoft.Teams.Plugins.AspNetCore.DevTools` | Not available |
| `Microsoft.Teams.Plugins.External.Mcp` | Not available — plugin architecture removed |
| `Microsoft.Teams.Plugins.External.McpClient` | Not available — plugin architecture removed |
| `Microsoft.Teams.Apps.Testing` | Not available — use standard DI mocking instead of `TestPlugin` |

### Old assemblies replaced by standard .NET

| Old Assembly | Replaced By |
|---|---|
| `Microsoft.Teams.Common` (logging) | `Microsoft.Extensions.Logging` |
| `Microsoft.Teams.Common` (HTTP) | `System.Net.Http.HttpClient` + DI |
| `Microsoft.Teams.Common` (storage) | No direct replacement — `IStorage<K,V>` removed |
| `Microsoft.Teams.Extensions.Configuration` | `Microsoft.Extensions.Configuration` via `BotConfig` |
| `Microsoft.Teams.Extensions.Logging` | `Microsoft.Extensions.Logging` (no bridge needed) |
| `Microsoft.Teams.Extensions.Hosting` | `TeamsBotApplicationHostingExtensions` |
| `Microsoft.Teams.Plugins.AspNetCore` | Standard ASP.NET Core middleware + `BotApplication.ProcessAsync()` |
| `Microsoft.Teams.Plugins.AspNetCore.BotBuilder` | `Microsoft.Teams.Apps.BotBuilder` (compat layer) |

---

## Quick Reference

| Old API | New API | Notes |
|---------|---------|-------|
| `builder.AddTeams()` | `builder.Services.AddTeamsBotApplication()` | `AddTeams` shims are `[Obsolete]` |
| `endpoints.UseTeams()` | `endpoints.UseTeamsBotApplication()` | `UseTeams` alias is `[Obsolete]` |
| `context.Send("text", ct)` | `context.SendAsync("text", ct)` | `Send` alias is `[Obsolete]` |
| `context.Send(activity, ct)` | `context.SendAsync(activity, ct)` | `Send` alias is `[Obsolete]` |
| `context.Reply("text", ct)` | `context.ReplyAsync("text", ct)` | `Reply` alias is `[Obsolete]` |
| `context.Reply(activity, ct)` | `context.ReplyAsync(activity, ct)` | `Reply` alias is `[Obsolete]` |
| `context.Typing(ct)` | `context.TypingAsync(ct)` | `Typing` alias is `[Obsolete]` |
| `context.Log.Info(...)` | `logger.LogInformation(...)` | DI `ILogger`; `Log` shim is `[Obsolete]` |
| `context.Log.Error(...)` | `logger.LogError(...)` | DI `ILogger`; `Log` shim is `[Obsolete]` |
| `context.Log.Debug(...)` | `logger.LogDebug(...)` | DI `ILogger`; `Log` shim is `[Obsolete]` |
| `context.Log.Warn(...)` | `logger.LogWarning(...)` | DI `ILogger`; `Log` shim is `[Obsolete]` |
| `context.AppId` | `context.AppId` | Same API |
| `teams.Send(convId, text)` | `teams.SendAsync(convId, text)` | Proactive messaging; `Send` alias is `[Obsolete]` |
| `teams.Reply(convId, msgId, text)` | `teams.ReplyAsync(convId, msgId, text)` | Proactive threaded reply; `Reply` alias is `[Obsolete]` |
| `InvokeResponse(200, body)` | `InvokeResponse.Ok(body)` | Factory method available |
| `InvokeResponse(400, body)` | `InvokeResponse.Error(400, body)` | Factory method available |
| `App.Builder().AddOAuth("graph")` | `AddTeamsBotApplication(o => o.AddOAuthFlow("graph"))` | Old shim is `[Obsolete]` |

---

## ✅ Backward-Compatible Changes (No Migration Needed)

These APIs have been added to the new library to match the old API surface. Existing code using these patterns will work without changes.

### Context Convenience Methods (BC-1)

The following methods are available on `Context<TActivity>`:

```csharp
// Send a text message
await context.SendAsync("Hello!", cancellationToken);

// Send an activity
await context.SendAsync(myActivity, cancellationToken);

// Send a threaded reply (auto-quotes the inbound message)
await context.ReplyAsync("This is a reply", cancellationToken);
await context.ReplyAsync(myActivity, cancellationToken);

// Quote a specific message by ID
await context.QuoteAsync(messageId, "Confirming the change", cancellationToken);

// Send typing indicator
await context.TypingAsync(cancellationToken);
```

> **Deprecated aliases:** The non-suffixed convenience methods `Send`, `Reply`, `Typing`, and `Quote` (added to match the old API surface) are marked `[Obsolete]` — use the `Async`-suffixed names above. The same applies to `context.SignIn` / `context.SignOut` (use `SignInAsync` / `SignOutAsync`).
>
> **Note:** `Send(AdaptiveCard)` and `Reply(AdaptiveCard)` are not yet available to avoid a dependency on `Microsoft.Teams.Cards`. Use `TeamsActivityBuilder` with `AddAdaptiveCardAttachment()` instead.

### Context Logger (BC-2)

Obtain a standard `Microsoft.Extensions.Logging.ILogger` via dependency injection and use its extension methods:

```csharp
ILogger logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("MyBot");

logger.LogInformation("Processing message");
logger.LogError("Something failed: {Error}", ex.Message);
logger.LogDebug("Activity ID: {Id}", context.Activity.Id);
```

> **Deprecated:** `context.Log` (the `ContextLogger` shim with `.Info()` / `.Error()` / `.Debug()` / `.Warn()` added to match the old API) is marked `[Obsolete]`. Use a standard `ILogger` obtained via DI instead — there is no logger property on `Context`.

### Context AppId (BC-5)

```csharp
var appId = context.AppId; // reads from TeamsBotApplication.AppId
```

### WebApplicationBuilder.AddTeams() (BC-7)

```csharp
// Recommended
builder.Services.AddTeamsBotApplication();
...
app.UseTeamsBotApplication();
```

> **Deprecated:** The backward-compatibility shims `AddTeams()` (on both `WebApplicationBuilder` and `IServiceCollection`) and `UseTeams()` (on `IEndpointRouteBuilder`) are marked `[Obsolete]`. Use `AddTeamsBotApplication()` and `UseTeamsBotApplication()` instead.

### Proactive Messaging (BC-8)

```csharp
// Send proactively to a conversation
await teams.SendAsync(conversationId, "Hello!", cancellationToken: ct);

// Send a threaded reply proactively
await teams.ReplyAsync(conversationId, messageId, "Replying!", ct);
```

> **Deprecated aliases:** The non-suffixed `teams.Send` / `teams.Reply` are marked `[Obsolete]` — use `SendAsync` / `ReplyAsync`.
>
> **Note:** The service URL is automatically cached from incoming activities. If you need to send proactively before any activity has been received, pass a `serviceUrl` parameter to `SendAsync()`.

### InvokeResponse Factory Methods (BC-12)

```csharp
// Instead of: new InvokeResponse(200, body)
return InvokeResponse.Ok(body);

// Typed version
return InvokeResponse.Ok<TaskModuleResponse>(response);

// Error responses
return InvokeResponse.Error(400, errorDetails);
```

### MessageActivity Fluent Methods (BC-15)

Extension methods on `MessageActivity`:

```csharp
var msg = new MessageActivity("hello")
    .WithSuggestedActions(actions)
    .WithAttachmentLayout("carousel")
    .AddAttachment(attachment1, attachment2);
```

Available: `WithText()`, `AddText()`, `WithSuggestedActions()`, `WithTextFormat()`, `WithAttachmentLayout()`, `AddAttachment()`, `AddStreamFinal()`.

Not migrated (low priority): `WithSummary()`, `WithDeliveryMode()`, `Merge()`. Not migrated (deprecated in old lib): `WithSpeak()`, `WithInputHint()`, `WithImportance()`, `WithExpiration()`.

### Activity Entity Methods

Entity getter helpers are exposed via entity-scoped extension methods:

```csharp
// Retrieve entity collections
activity.GetMentions();             // IEnumerable<MentionEntity>
activity.GetQuotedMessages();       // IEnumerable<QuotedReplyEntity>
activity.GetSensitivityLabels();    // IEnumerable<SensitiveUsageEntity>

// Retrieve single entities
activity.GetClientInfo();           // ClientInfoEntity?
activity.GetCitation();             // CitationEntity?
activity.GetStreamInfo();           // StreamInfoEntity?
activity.GetTargetedMessageInfo();  // TargetedMessageInfoEntity? (ExperimentalTeamsTargeted)
activity.GetProductInfo();          // ProductInfoEntity?
activity.GetMessageEntity();        // OMessageEntity?
```

All Get* methods are extension methods defined in the respective entity files (e.g., `GetMentions` is in `MentionEntityExtensions`).

### App.Builder() Pattern (BC-6)

> **Deprecated:** `App.Builder()`, the `AppBuilder` class, and the `AddTeams(WebApplicationBuilder, AppBuilder)` overload are marked `[Obsolete]`. They exist only so old `App.Builder().AddOAuth(...)` code compiles, and will be removed in a future release. Configure OAuth flows directly through DI instead:
>
> ```csharp
> builder.Services.AddTeamsBotApplication(options => options.AddOAuthFlow("graph"));
> ```

`App.Builder()` is still supported (with a deprecation warning) for `AddOAuth()`:

```csharp
// This works in both old and new libraries, but is deprecated in the new one:
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

## 📝 Breaking Changes Requiring Migration

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

> **Deprecated:** The per-turn OAuth helpers on `Context` (`SignInAsync`, `SignOutAsync`, `IsSignedInAsync`, `GetConnectionStatusAsync`, and the older `SignIn` / `SignOut`) are marked `[Obsolete]`. Resolve the flow once via `teams.GetOAuthFlow(connectionName)` and call the corresponding method on it, passing `context`:
>
> ```csharp
> OAuthFlow auth = teams.GetOAuthFlow("graph");
> string? token = await auth.SignInAsync(context, ct);
> await auth.SignOutAsync(context, ct);
> bool signedIn = await auth.IsSignedInAsync(context, ct);
> ```

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

### BC-17: Activity fluent `With*()` methods moved to builder/extensions

**Old:**
```csharp
var activity = new Activity().WithFrom(account).WithConversation(conv);
```

**New (recommended builder):**
```csharp
var activity = new TeamsActivityBuilder()
    .WithFrom(account)
    .WithConversation(conv)
    .Build();
```

**New (extension methods on `MessageActivity`):**
```csharp
var activity = new MessageActivity()
    .WithFrom(account)
    .WithConversation(conv)
    .WithChannelId("msteams");
```

Most base `With*()` methods are available as extension methods in `MessageActivityExtensions` (for example `WithId`, `WithChannelId`, `WithFrom`, `WithRecipient`, `WithConversation`, `WithServiceUrl`, `WithLocale`, `WithTimestamp`, `WithLocalTimestamp`, `WithData`, and `WithAppId`).

`WithRelatesTo` is unavailable because there is no `ConversationReference` equivalent in core (and `RelatesTo` is `[Obsolete]` in the old library).

---

### BC-18: Activity conversion methods replaced by factories

**Old:**
```csharp
var msg = activity.ToMessage();
```

**New:**
```csharp
var msg = MessageActivity.FromActivity(coreActivity);
```

---

### BC-21: Type incompatibilities

These are intentional architectural changes — adjust call sites accordingly.

| Property | Old Type | New Type |
|---|---|---|
| `Timestamp`, `LocalTimestamp` | `DateTime?` | `string?` |
| `ServiceUrl` | `string?` | `Uri?` |
| `ContentUrl`, `ThumbnailUrl` (Attachment) | `string?` | `Uri?` |
| Enums (`TextFormat`, `InputHint`, etc.) | Enum types | String constants |
| `Account` | Custom `Account` class | `ConversationAccount` |

---

### BC-22: `Conversation.ToThreadedConversationId()` moved

Available as `Microsoft.Teams.Core.Schema.ConversationExtensions.ToThreadedConversationId(conversationId, messageId)`. In the old library this was a static method on the `Conversation` class; in the new library it lives on `ConversationExtensions` in the `Microsoft.Teams.Core.Schema` namespace. The functionality is unchanged — only a namespace/type adjustment is needed for direct callers. `TeamsBotApplication.Reply()` uses it internally.

---

### BC-24: `SuggestedActions` fluent methods removed

The old `SuggestedActions` had `AddRecipients()`, `AddAction()`, `AddActions()` fluent methods. Use direct property assignment instead.

---

### Hosting and Plugin Architecture

The old plugin-based architecture is entirely removed. This affects:

| Old Pattern | New Equivalent |
|---|---|
| `ISenderPlugin` / `IAspNetCorePlugin` | Not available — use `TeamsBotApplication` directly |
| `AddTeamsPlugin<T>()` | Not available — register services via standard DI |
| `TeamsService` (IHostedService) | Not needed — lifecycle managed by `BotApplication.ProcessAsync()` |
| `AddTeamsTokenAuthentication()` | Built into `AddTeamsBotApplication()` via `BotConfig` |
| `TeamsValidationSettings` | Replaced by `JwtExtensions` + `BotConfig` |
| `AspNetCorePlugin.Configure()` | Use standard `app.UseAuthentication()` / `app.UseAuthorization()` |

---

### Common library replacements

| Old Type | New Equivalent |
|---|---|
| `Microsoft.Teams.Common.Logging.ILogger` | `Microsoft.Extensions.Logging.ILogger` |
| `Microsoft.Teams.Common.Logging.ConsoleLogger` | `builder.Logging.AddConsole()` |
| `Microsoft.Teams.Common.Logging.LogLevel` | `Microsoft.Extensions.Logging.LogLevel` |
| `Microsoft.Teams.Common.Http.IHttpClient` | `System.Net.Http.HttpClient` via DI |
| `Microsoft.Teams.Common.Http.IHttpClientFactory` | `Microsoft.Extensions.Http.IHttpClientFactory` |
| `Microsoft.Teams.Common.Http.HttpException` | `System.Net.Http.HttpRequestException` |
| `Microsoft.Teams.Common.Storage.IStorage<K,V>` | No direct replacement — removed from SDK |
| `Microsoft.Teams.Common.Storage.LocalStorage<V>` | No direct replacement — use `IMemoryCache` or custom |

---

### Testing

The old `TestPlugin` from `Microsoft.Teams.Apps.Testing` is not available. Use standard .NET testing patterns:

```csharp
// Old: TestPlugin-based
var plugin = new TestPlugin();
var app = App.Builder().AddPlugin(plugin).Build();

// New: Direct instantiation with mocks
var mockBot = new Mock<TeamsBotApplication>(...);
var context = new Context<MessageActivity>(mockBot.Object, activity);
```

---

## ⚠️ Under Review / Not Yet Available

The following items are being evaluated and may change:

- **BC-1 (partial):** `Send(AdaptiveCard)` / `Reply(AdaptiveCard)` — pending Teams.Cards dependency decision.
- **BC-3:** Middleware / `OnActivity` / `Use()` / `Next()` — no middleware pipeline yet; the router dispatches directly to matching routes. Needs investigation into whether affected samples require the Teams `Context<TActivity>` inside middleware.
- **BC-11:** `OnSetting()` message-extension handler — activity type clarification needed.
- **BC-14:** `app.AddTab()` — scope of feature TBD (static file serving vs. tab config endpoints).
- **BC-19:** Missing activity types. `TypingActivity` (typing is handled via `TeamsActivityType.Typing`); `EndOfConversationActivity`, `CommandActivity` / `CommandResultActivity` were deprecated in the old library, so omitting them is not a breaking change.
- **BC-20:** Missing handler registration methods — Tab handlers (`OnTabFetch`, `OnTabSubmit`, `OnConfigFetch`, `OnConfigSubmit`), infrastructure events (`OnActivity`, `OnError`, `OnStart`, `OnActivityResponse`, `OnActivitySent`), and commented-out handlers (`OnSetting`, `OnCardButtonClicked`, `OnTypeaheadSearch`, `OnAnswerSearch`, `OnReadReceipt`). `OnCommand` / `OnCommandResult` were deprecated in the old library.
- **BC-23:** `MessageActivity` commented-out properties. Deprecated in old lib (safe to omit): `Speak`, `InputHint`, `Importance`, `Expiration`. Still active in old lib (real gap): `Summary`, `DeliveryMode`, `Value`.

---

## Verification

When closing out a pending item:

1. Build `core/src/Microsoft.Teams.Apps` to verify compilation across target frameworks.
2. Verify existing tests pass.
3. Trace the migration of each affected sample to confirm the gap is closed; migrate `Samples.Echo` end-to-end as the proof-of-concept for the full compat surface.
