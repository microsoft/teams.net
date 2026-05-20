# Migration Guide: Libraries/Microsoft.Teams.Apps to core/src/Microsoft.Teams.Apps

This guide covers migrating from the old `Microsoft.Teams.Apps` library (`Libraries/`) to the new `Microsoft.Teams.Apps` library (`core/src/`).

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

### MessageActivity Fluent Methods (BC-15)

Extension methods on `MessageActivity`:

```csharp
var msg = new MessageActivity("hello")
    .WithSuggestedActions(actions)
    .WithAttachmentLayout("carousel")
    .AddAttachment(attachment1, attachment2);
```

Available: `WithText()`, `WithSuggestedActions()`, `WithTextFormat()`, `WithAttachmentLayout()`, `AddAttachment()`, `AddStreamFinal()`.

### Activity Entity Methods

Entity getter helpers are exposed via entity-scoped extension methods:

```csharp
// Retrieve entity collections
activity.GetMentions();             // IEnumerable<MentionEntity>
activity.GetQuotedMessages();       // IEnumerable<QuotedReplyEntity> (ExperimentalTeamsQuotedReplies)
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

`WithRelatesTo` is still unavailable because there is no `ConversationReference` equivalent in core.

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

| Property | Old Type | New Type |
|---|---|---|
| `Timestamp`, `LocalTimestamp` | `DateTime?` | `string?` |
| `ServiceUrl` | `string?` | `Uri?` |
| `ContentUrl`, `ThumbnailUrl` (Attachment) | `string?` | `Uri?` |
| Enums (`TextFormat`, `InputHint`, etc.) | Enum types | String constants |
| `Account` | Custom `Account` class | `ConversationAccount` / `TeamsConversationAccount` |

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

## Items Under Review

The following items are being evaluated and may change:

- **BC-1 (partial):** `Send(AdaptiveCard)` / `Reply(AdaptiveCard)` — pending Teams.Cards dependency decision
- **BC-3:** Middleware / `OnActivity` / `Use()` / `Next()` — architecture review needed
- **BC-11:** `OnSetting()` message extension handler — activity type clarification needed
- **BC-14:** `AddTab()` — scope of feature TBD
- **BC-19:** Missing activity types (`TypingActivity`, `EndOfConversationActivity`, `CommandActivity`)
- **BC-20:** Missing handler registration methods (Tab, Command, Infrastructure, commented-out handlers)
- **BC-22:** `Conversation.ToThreadedConversationId()` static utility
- **BC-23:** MessageActivity commented-out properties (`Speak`, `InputHint`, `Summary`, `Importance`, `DeliveryMode`, `Expiration`)
