# Plan: Reduce Breaking Changes between Libraries/Microsoft.Teams.Apps and core/src/Microsoft.Teams.Apps

## Context

The `core/src/Microsoft.Teams.Apps` project is the next version of `Libraries/Microsoft.Teams.Apps`. The current samples all target the old library. This plan identifies every public API breaking change and proposes concrete changes to the **new library** to minimize migration friction, prioritized by impact across samples.

---

## Breaking Changes Inventory

### BC-1: Context convenience methods removed (ALL 13 samples affected)

**Decision: IMPLEMENT** (defer `Send(AdaptiveCard)` — avoid Teams.Cards dependency for now)

**Old API:**
```csharp
await context.Send("text", cancellationToken);
await context.Send(card, cancellationToken);
await context.Reply("text", cancellationToken);
await context.Reply(card, cancellationToken);
await context.Typing("processing", cancellationToken);
```

**New API:**
```csharp
await context.SendActivityAsync("text", cancellationToken);  // only string overload
// No Send(AdaptiveCard), no Reply(), no Typing()
```

**Samples affected:** Echo, Cards, Dialogs, Graph, Meetings, MessageExtensions, Reactions, TargetedMessages, Threading, Tab, Lights

**Proposed fix:** Add convenience methods to `Context<TActivity>`:
- `Send(string text, CancellationToken)` -> wraps `SendActivityAsync(text)`
- `Send(TeamsActivity activity, CancellationToken)` -> wraps `SendActivityAsync(activity)`
- ~~`Send(AdaptiveCard card, CancellationToken)`~~ **DEFERRED** — review later to avoid Teams.Cards dependency
- `Reply(string text, CancellationToken)` -> builds threaded reply activity
- `Reply(TeamsActivity activity, CancellationToken)` -> builds threaded reply
- ~~`Reply(AdaptiveCard card, CancellationToken)`~~ **DEFERRED** — same reason
- `Typing(string? text, CancellationToken)` -> wraps `SendTypingActivityAsync()`

**File:** `core/src/Microsoft.Teams.Apps/Context.cs`

---

### BC-2: No `context.Log` logger (12 samples affected)

**Decision: IMPLEMENT** — `context.Log` with `.Info()`, `.Error()`, `.Debug()` delegating to `ILogger`

**Old API:**
```csharp
context.Log.Info("message");
context.Log.Error("error");
context.Log.Debug("debug");
```

**New API:** No logger on context at all.

**Samples affected:** Echo, Cards, Dialogs, Graph, Meetings, MessageExtensions, Reactions, TargetedMessages, Tab, Lights, BotBuilder, Deprecated.Controllers

**Proposed fix:** Add `Log` property to `Context<TActivity>` that exposes an object with `.Info()`, `.Error()`, `.Debug()` methods, delegating to `Microsoft.Extensions.Logging.ILogger` sourced from DI. This preserves the old API surface while using the standard logging infrastructure.

**File:** `core/src/Microsoft.Teams.Apps/Context.cs`

---

### BC-3: No middleware / `OnActivity` + `Use()` + `context.Next()` (5 samples affected)

**Decision: REVIEW LATER**

**Old API:**
```csharp
teams.Use(async context => {
    // before
    await context.Next();
    // after
});
teams.OnActivity(async (context, cancellationToken) => {
    context.Log.Info(context.AppId);
    await context.Next();
});
```

**New API:** No middleware pipeline. Router dispatches directly to matching routes. No `Next()` on context.

**Samples affected:** Echo, Graph, Meetings, TargetedMessages (OnActivity as catch-all)

**Note:** Need to investigate whether affected samples use `context.Next()` in a way that requires access to the Teams `Context<TActivity>` inside the middleware. If so, ASP.NET middleware won't be a sufficient replacement.

**File:** New extension method in `core/src/Microsoft.Teams.Apps/Handlers/`

---

### BC-4: No `context.Ref` (ConversationReference) (2 samples affected)

**Decision: DOC-ONLY**

**Old API:**
```csharp
var conversationId = context.Ref.Conversation.Id;
```

**New API:** No `Ref` property. Must use `context.Activity.Conversation.Id` directly.

**Samples affected:** Threading, TargetedMessages

**Migration:** `context.Ref.Conversation.Id` -> `context.Activity.Conversation.Id`

---

### BC-5: No `context.AppId` (1 sample affected)

**Decision: IMPLEMENT**

**Old API:**
```csharp
context.Log.Info(context.AppId);
```

**New API:** No `AppId` on context.

**Samples affected:** Echo

**Proposed fix:** Add `AppId` property to `Context<TActivity>` reading from `TeamsBotApplication.AppId`.

**File:** `core/src/Microsoft.Teams.Apps/Context.cs`

---

### BC-6: App.Builder() pattern removed (2 samples affected)

**Decision: IMPLEMENT** — Add `App.Builder()` as a wrapper around ASP.NET DI

**Old API:**
```csharp
var appBuilder = App.Builder()
    .AddLogger(new ConsoleLogger(...))
    .AddOAuth("graph");
builder.AddTeams(appBuilder);
```

**New API:**
```csharp
builder.Services.AddTeamsBotApplication(options => {
    options.AddOAuthFlow("graph");
});
```

**Samples affected:** Graph, Meetings

**Proposed fix:** Add `App.Builder()` that wraps the standard ASP.NET DI options pattern, providing a clear migration path from the old builder API.

---

### BC-7: `AddTeams()` extension target changed (ALL samples affected)

**Decision: IMPLEMENT** — parameterless overload only

**Old API:**
```csharp
builder.AddTeams();           // on WebApplicationBuilder
builder.AddTeams(appBuilder); // with App.Builder
```

**New API:**
```csharp
builder.Services.AddTeams();  // on IServiceCollection
```

**Samples affected:** All

**Proposed fix:** Add parameterless extension method on `WebApplicationBuilder` that delegates to `builder.Services.AddTeams()`.

**File:** `core/src/Microsoft.Teams.Apps/TeamsBotApplication.HostingExtensions.cs`

---

### BC-8: Proactive messaging from app-level (1 sample affected)

**Decision: IMPLEMENT**

**Old API:**
```csharp
await teams.Send(conversationId, "text", cancellationToken: ct);
await teams.Reply(conversationId, messageId, "text", ct);
```

**New API:** No `Send()`/`Reply()` convenience methods on `TeamsBotApplication`.

**Samples affected:** Threading

**Proposed fix:** Add convenience methods on `TeamsBotApplication`:
- `Send(string conversationId, string text, ...)`
- `Reply(string conversationId, string messageId, string text, ...)`

**File:** `core/src/Microsoft.Teams.Apps/TeamsBotApplication.cs` or extension

---

### BC-9: `OnSignIn` / `OnSignInFailure` events removed (1 sample affected)

**Decision: DOC-ONLY** — existing `context.OnSignIn` methods provide backward compat

**Old API:**
```csharp
teams.OnSignIn(async (_, @event, cancellationToken) => { ... });
teams.OnSignInFailure(async (context, cancellationToken) => { ... });
```

**New API:** Uses `OAuthFlow.OnSignInComplete()` and `OAuthFlow.OnSignInFailure()` callbacks.

**Samples affected:** Graph

**Migration:** The new pattern uses per-flow callbacks. Existing `context.OnSignIn` methods cover backward compat:
```csharp
var flow = teams.GetOAuthFlow("graph");
flow.OnSignInComplete(handler);
flow.OnSignInFailure(handler);
```

---

### BC-10: Meeting handler renames (1 sample affected)

**Decision: IMPLEMENT** — aliases without `[Obsolete]`

**Old API:**
```csharp
teams.OnMeetingJoin(handler);
teams.OnMeetingLeave(handler);
```

**New API:**
```csharp
teams.OnMeetingParticipantJoin(handler);
teams.OnMeetingParticipantLeave(handler);
```

**Samples affected:** Meetings

**Proposed fix:** Add `OnMeetingJoin()` and `OnMeetingLeave()` as aliases that call the new methods. No `[Obsolete]` attribute for now — will revisit later.

**File:** `core/src/Microsoft.Teams.Apps/Handlers/MeetingExtensions.cs`

---

### BC-11: `OnSetting()` handler missing (1 sample affected)

**Decision: REVIEW LATER**

**Old API:**
```csharp
teams.OnSetting((context, cancellationToken) => { ... });
```

**New API:** No `OnSetting()` extension method.

**Samples affected:** MessageExtensions

**Note:** Need to clarify what activity type/invoke name `OnSetting()` matches before implementing.

**File:** `core/src/Microsoft.Teams.Apps/Handlers/MessageExtension/MessageExtensionExtensions.cs`

---

### BC-12: Invoke handler return types changed (4 samples affected)

**Decision: IMPLEMENT** — factory methods only (no implicit conversions)

**Old API:** Handlers return library-specific response types:
```csharp
// AdaptiveCardAction returns ActionResponse
return new ActionResponse.Message("text") { StatusCode = 400 };

// TaskFetch/Submit returns Microsoft.Teams.Api.TaskModules.Response
return new Microsoft.Teams.Api.TaskModules.Response(...);

// MessageExtension returns Microsoft.Teams.Api.MessageExtensions.Response
return response;
```

**New API:** Handlers return `InvokeResponse` or `InvokeResponse<T>`:
```csharp
Task<InvokeResponse> AdaptiveCardActionHandler(...)
Task<InvokeResponse<TaskModuleResponse>> TaskModuleHandler(...)
Task<InvokeResponse<MessageExtensionResponse>> MessageExtensionQueryHandler(...)
```

**Samples affected:** Cards, Dialogs, MessageExtensions, Lights

**Proposed fix:** Add factory methods:
- `InvokeResponse.Ok(body)` — wraps body with 200 status
- `InvokeResponse.Error(status, body)` — wraps body with error status

**File:** Invoke response types in core project

---

### BC-13: Activity type hierarchy changed (MEDIUM - affects type usage)

**Decision: DOC-ONLY**

**Old:** Activities come from `Microsoft.Teams.Api.Activities` (e.g., `MessageActivity`, `InvokeActivity`)
**New:** Activities come from `Microsoft.Teams.Apps.Schema` (e.g., `MessageActivity : TeamsActivity`)

**Migration:** Namespace imports change but member access stays the same. Provide namespace mapping table in migration docs.

---

### BC-14: `app.AddTab()` missing (1 sample affected)

**Decision: REVIEW LATER**

**Old API:**
```csharp
app.AddTab("dialog-form", "Web/dialog-form");
```

**New API:** No `AddTab()` method.

**Samples affected:** Dialogs, Tab

**Note:** Need to determine if `AddTab()` is just static file serving or also registers Teams tab config endpoints. This affects whether a simple "use `app.UseStaticFiles()`" migration note is sufficient.

---

## Sample Migration Difficulty Assessment

| Sample | Difficulty | Key Blockers |
|--------|-----------|-------------|
| Samples.Echo | **Easy** | BC-1 (Send/Typing), BC-2 (Log), BC-3 (OnActivity/Next), BC-5 (AppId), BC-7 (AddTeams) |
| Samples.Cards | **Easy** | BC-1 (Send), BC-2 (Log), BC-7, BC-12 (InvokeResponse) |
| Samples.Reactions | **Easy** | BC-1 (Send), BC-2 (Log), BC-7 |
| Samples.Threading | **Easy** | BC-1 (Send/Reply), BC-4 (Ref), BC-7, BC-8 (proactive) |
| Samples.Dialogs | **Easy-Med** | BC-1 (Send), BC-2 (Log), BC-7, BC-12, BC-14 (AddTab) |
| Samples.MessageExtensions | **Easy-Med** | BC-1, BC-2, BC-7, BC-11 (OnSetting), BC-12 |
| Samples.TargetedMessages | **Easy-Med** | BC-1 (Send/Reply), BC-2, BC-3 (OnActivity), BC-7 |
| Samples.Meetings | **Medium** | BC-1, BC-2, BC-3 (Use/Next), BC-6 (Builder), BC-7, BC-10 (renames) |
| Samples.Graph | **Medium-Hard** | BC-1, BC-2, BC-3 (Use/Next), BC-6 (Builder), BC-7, BC-9 (SignIn events) |
| Samples.BotBuilder | **Hard** | Entire `AddBotBuilder<>()` pattern missing |
| Deprecated.Controllers | **N/A** | Already deprecated, no migration needed |

---

## Implementation Plan (ordered by impact)

### Item 1: Add `Send()`, `Reply()`, `Typing()` convenience methods to Context
- **File:** `core/src/Microsoft.Teams.Apps/Context.cs`
- **Impact:** Resolves BC-1, unblocks ALL samples
- **Details:** Add methods matching old signatures (except AdaptiveCard overloads — deferred). `Reply()` builds a threaded reply using `Activity.Conversation.Id` and `Activity.Id`.

### Item 2: Add `context.Log` with `.Info()`, `.Error()`, `.Debug()` delegating to ILogger
- **File:** `core/src/Microsoft.Teams.Apps/Context.cs`
- **Impact:** Resolves BC-2, unblocks 12 samples
- **Details:** Expose a `Log` property with `.Info()`, `.Error()`, `.Debug()` methods that delegate to `Microsoft.Extensions.Logging.ILogger` from DI. Preserves old API surface.

### Item 3: Add `WebApplicationBuilder.AddTeams()` extension
- **File:** `core/src/Microsoft.Teams.Apps/TeamsBotApplication.HostingExtensions.cs`
- **Impact:** Resolves BC-7, unblocks ALL samples
- **Details:** Parameterless `public static IServiceCollection AddTeams(this WebApplicationBuilder builder) => builder.Services.AddTeams();`

### Item 4: Add `AppId` property to Context
- **File:** `core/src/Microsoft.Teams.Apps/Context.cs`
- **Impact:** Resolves BC-5
- **Details:** `AppId` sourced from `TeamsBotApplication.AppId`.

### Item 5: Add `App.Builder()` wrapper around ASP.NET DI
- **Impact:** Resolves BC-6, unblocks Graph and Meetings samples
- **Details:** `App.Builder()` returns a builder that wraps standard ASP.NET DI options pattern.

### Item 6: Add `OnMeetingJoin`/`OnMeetingLeave` aliases
- **File:** `core/src/Microsoft.Teams.Apps/Handlers/MeetingExtensions.cs`
- **Impact:** Resolves BC-10
- **Details:** Aliases to `OnMeetingParticipantJoin`/`OnMeetingParticipantLeave`. No `[Obsolete]` for now.

### Item 7: Add proactive `Send()`/`Reply()` on TeamsBotApplication
- **File:** `core/src/Microsoft.Teams.Apps/TeamsBotApplication.cs` or extension
- **Impact:** Resolves BC-8, unblocks Threading sample

### Item 8: Add `InvokeResponse.Ok()`/`InvokeResponse.Error()` factory methods
- **Impact:** Resolves BC-12
- **File:** Invoke response types in core project

### Item 9: Document migration for architectural changes (no code)
- `context.Ref` (BC-4): `context.Ref.Conversation.Id` -> `context.Activity.Conversation.Id`
- SignIn events (BC-9): `teams.OnSignIn()` -> `flow.OnSignInComplete()` (existing context.OnSignIn covers compat)
- Namespace changes (BC-13): Provide mapping table

---

## API Surface Gaps (from systematic comparison)

These were identified by comparing the full public API surface between old and new libraries, beyond what the sample-driven analysis caught.

### BC-15: MessageActivity fluent methods removed

**Decision: IMPLEMENTED** — MessageActivity fluent extension methods added.

Methods added: `WithText()`, `AddText()`, `WithSuggestedActions()`, `WithTextFormat()`, `WithAttachmentLayout()`, `AddAttachment()`, `AddStreamFinal()`.

Not migrated (low priority, underlying properties commented out): `WithSpeak()`, `WithInputHint()`, `WithSummary()`, `WithImportance()`, `WithDeliveryMode()`, `WithExpiration()`, `Merge()`.

---

### BC-16: `AddSensitivityLabel()` missing on TeamsActivity

**Decision: IMPLEMENTED** — Extension method added in `TeamsActivityExtensions`.

---

### BC-17: Base Activity fluent `With*()` methods removed

**Decision: IMPLEMENTED (mostly)** — Base activity fluent methods added.

- **With* methods:** `WithId`, `WithChannelId`, `WithFrom`, `WithRecipient`, `WithRecipient(..., bool isTargeted)`, `WithConversation`, `WithServiceUrl`, `WithLocale`, `WithTimestamp`, `WithLocalTimestamp`, `WithData(ChannelData)`, `WithData(string, object?)`, `WithAppId`
- **Add* methods:** `AddEntity`, `UpdateEntity`, `AddAIGenerated`, `AddFeedback(bool)`, `AddTargetedMessageInfo`, `AddCitation`, `AddMention`, `AddSensitivityLabel`, `AddClientInfo`
- **Get* methods:** `GetAccountMention`

Remaining gap: `WithRelatesTo` is still not migrated because core currently has no `ConversationReference` model.

---

### BC-18: Activity conversion methods removed (`ToMessage()`, `ToInvoke()`, etc.)

**Decision: NOT MIGRATED** — The old library had `ToMessage()`, `ToInvoke()`, `ToEvent()`, etc. The new library uses `FromActivity()` static factory methods instead:

```csharp
// Old: activity.ToMessage()
// New: MessageActivity.FromActivity(coreActivity)
```

---

### BC-19: Missing activity types

**Decision: REVIEW LATER**

| Missing Type | Notes |
|---|---|
| `TypingActivity` | No class in new lib; typing handled via `TeamsActivityType.Typing` |
| `EndOfConversationActivity` | Commented out / TODO |
| `CommandActivity` / `CommandResultActivity` | Commented out / TODO |
| `ConversationReference` | Entire class missing; no direct replacement |

---

### BC-20: Missing handler registration methods

**Decision: REVIEW LATER** — 18 handler methods exist in the old library but not in the new.

**Tab handlers (completely removed):**
- `OnTabFetch`, `OnTabSubmit`, `OnConfigFetch`, `OnConfigSubmit`

**Command handlers (removed):**
- `OnCommand`, `OnCommandResult`

**Infrastructure events (architectural change):**
- `OnActivity`, `OnError`, `OnStart`, `OnActivityResponse`, `OnActivitySent`

**Auth events (restructured to per-flow):**
- `OnSignIn`, `OnSignInFailure`, `OnTokenExchange`, `OnVerifyState`

**Other removed handlers:**
- `OnTyping`, `OnHandoff`, `OnFeedback`, `OnExecuteAction`

**Commented out in new library:**
- `OnSetting`, `OnCardButtonClicked`, `OnTypeaheadSearch`, `OnAnswerSearch`, `OnReadReceipt`

---

### BC-21: Type incompatibilities

**Decision: NOT MIGRATED** — Intentional architectural changes.

| Property | Old Type | New Type |
|---|---|---|
| `Timestamp`, `LocalTimestamp` | `DateTime?` | `string?` |
| `ServiceUrl` | `string?` | `Uri?` |
| `ContentUrl`, `ThumbnailUrl` (Attachment) | `string?` | `Uri?` |
| Enums (`TextFormat`, `InputHint`, etc.) | Enum types | String constants |
| `Account` | Custom `Account` class | `ConversationAccount` |

---

### BC-22: `Conversation.ToThreadedConversationId()` missing

**Decision: REVIEW LATER** — Static utility method for constructing threaded conversation IDs. Used by Threading sample. The new `TeamsBotApplication.Reply()` handles this internally, but direct usage in sample code would break.

---

### BC-23: MessageActivity commented-out properties

**Decision: REVIEW LATER** — These properties exist in the old library but are commented out in the new:
`Speak`, `InputHint`, `Summary`, `Importance`, `DeliveryMode`, `Expiration`, `Value`

---

### BC-24: SuggestedActions fluent methods removed

**Decision: NOT MIGRATED** — Old `SuggestedActions` had `AddRecipients()`, `AddAction()`, `AddActions()` fluent methods. Use direct property assignment instead.

---

## Items to Review Later

- **BC-1 (partial):** `Send(AdaptiveCard)` / `Reply(AdaptiveCard)` — blocked on Teams.Cards dependency decision
- **BC-3:** Middleware / `OnActivity` / `Use()` / `Next()` — need to investigate sample usage patterns
- **BC-11:** `OnSetting()` handler — need to clarify activity type/invoke name
- **BC-14:** `AddTab()` — need to determine if it's static files only or also tab config endpoints
- **BC-19:** Missing activity types (`TypingActivity`, `EndOfConversationActivity`, `CommandActivity`)
- **BC-20:** Missing handler registration methods (Tab, Command, Infrastructure, commented-out)
- **BC-22:** `Conversation.ToThreadedConversationId()` static utility
- **BC-23:** MessageActivity commented-out properties

---

## Verification

After each item:
1. Build `core/src/Microsoft.Teams.Apps` to verify compilation
2. Verify existing tests pass (if any)
3. For each item, attempt to mentally trace migration of affected sample code to confirm the gap is closed

After all items:
1. Migrate Samples.Echo as proof-of-concept to validate the full compat surface
2. Run the migrated sample to verify end-to-end functionality
