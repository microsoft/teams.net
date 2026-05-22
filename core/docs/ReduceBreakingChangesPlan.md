# Plan: Reduce Breaking Changes between Libraries/Microsoft.Teams.Apps and core/src/Microsoft.Teams.Apps

## Context

The `core/src/Microsoft.Teams.Apps` project is the next version of `Libraries/Microsoft.Teams.Apps`. The current samples all target the old library. This plan identifies every public API breaking change and proposes concrete changes to the **new library** to minimize migration friction, prioritized by impact across samples.

---

## Breaking Changes Inventory

---

### Pending: Items to Review or Implement

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

### BC-19: Missing activity types

**Decision: REVIEW LATER**

| Missing Type | Notes |
|---|---|
| `TypingActivity` | No class in new lib; typing handled via `TeamsActivityType.Typing`. |
| `EndOfConversationActivity` | Deprecated in old lib — not a breaking change to omit. |
| `CommandActivity` / `CommandResultActivity` | Deprecated in old lib — not a breaking change to omit. |

---

### BC-20: Missing handler registration methods

**Decision: REVIEW LATER** — 18 handler methods exist in the old library but not in the new.

**Tab handlers (completely removed):**
- `OnTabFetch`, `OnTabSubmit`, `OnConfigFetch`, `OnConfigSubmit`

**Command handlers (not a breaking change):**
- `OnCommand`, `OnCommandResult` — fully deprecated in the old library (activity types + handlers). Not a breaking change to drop.

**Infrastructure events (architectural change):**
- `OnActivity`, `OnError`, `OnStart`, `OnActivityResponse`, `OnActivitySent`

**Auth events (restructured to per-flow):**
- `OnSignIn`, `OnSignInFailure`, `OnTokenExchange`, `OnVerifyState`

**Other removed handlers:**
-  `OnTyping` , `OnHandoff`, `OnFeedback`, `OnExecuteAction`

**Commented out in new library:**
- `OnSetting`, `OnCardButtonClicked`, `OnTypeaheadSearch`, `OnAnswerSearch`, `OnReadReceipt` — all active (not deprecated)

---

### BC-23: MessageActivity commented-out properties

**Decision: REVIEW LATER (partial)** — These properties exist in the old library but are commented out in the new.

**Deprecated in old lib (not a breaking change to omit):**
`Speak`, `InputHint`, `Importance`, `Expiration` — marked `[Obsolete("This will be removed by end of summer 2026.")]` in commit 6f33aba.

**Active in old lib (real breaking change):**
`Summary`, `DeliveryMode`, `Value` — not deprecated. Still need to be addressed.

---

### Doc-Only: Architectural Changes (No Code Change Needed)

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

### BC-13: Activity type hierarchy changed (MEDIUM - affects type usage)

**Decision: DOC-ONLY**

**Old:** Activities come from `Microsoft.Teams.Api.Activities` (e.g., `MessageActivity`, `InvokeActivity`)
**New:** Activities come from `Microsoft.Teams.Apps.Schema` (e.g., `MessageActivity : TeamsActivity`)

**Migration:** Namespace imports change but member access stays the same. Provide namespace mapping table in migration docs.

---

### Not Migrated: Intentional Decisions

---

### BC-18: Activity conversion methods removed (`ToMessage()`, `ToInvoke()`, etc.)

**Decision: NOT MIGRATED** — The old library had `ToMessage()`, `ToInvoke()`, `ToEvent()`, etc. The new library uses `FromActivity()` static factory methods instead:

```csharp
// Old: activity.ToMessage()
// New: MessageActivity.FromActivity(coreActivity)
```

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

### BC-24: SuggestedActions fluent methods removed

**Decision: NOT MIGRATED** — Old `SuggestedActions` had `AddRecipients()`, `AddAction()`, `AddActions()` fluent methods. Use direct property assignment instead.

---

### Implemented

---

### BC-1: Context convenience methods removed (ALL 13 samples affected)

**Decision: IMPLEMENTED** (defer `Send(AdaptiveCard)` — avoid Teams.Cards dependency for now)

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

**Fix applied:** Convenience methods added to `Context<TActivity>`:
- `Send(string text, CancellationToken)` -> delegates to `SendAsync`
- `Send(TeamsActivity activity, CancellationToken)` -> delegates to `SendAsync`
- ~~`Send(AdaptiveCard card, CancellationToken)`~~ **DEFERRED** — review later to avoid Teams.Cards dependency
- `Reply(string text, CancellationToken)` -> delegates to `ReplyAsync`
- `Reply(TeamsActivity activity, CancellationToken)` -> delegates to `ReplyAsync`
- ~~`Reply(AdaptiveCard card, CancellationToken)`~~ **DEFERRED** — same reason
- `Typing(string? text, CancellationToken)` -> delegates to `TypingAsync`

**File:** `core/src/Microsoft.Teams.Apps/Context.cs`

---

### BC-2: No `context.Log` logger (12 samples affected)

**Decision: IMPLEMENTED** — `context.Log` with `.Info()`, `.Error()`, `.Debug()` delegating to `ILogger`

**Old API:**
```csharp
context.Log.Info("message");
context.Log.Error("error");
context.Log.Debug("debug");
```

**Samples affected:** Echo, Cards, Dialogs, Graph, Meetings, MessageExtensions, Reactions, TargetedMessages, Tab, Lights, BotBuilder, Deprecated.Controllers

**Fix applied:** `Log` property added to `Context<TActivity>` as `ContextLogger`, exposing `.Info()`, `.Error()`, `.Debug()`, `.Warn()` methods delegating to `ILogger`.

**File:** `core/src/Microsoft.Teams.Apps/Context.cs`

---

### BC-5: No `context.AppId` (1 sample affected)

**Decision: IMPLEMENTED**

**Old API:**
```csharp
context.Log.Info(context.AppId);
```

**Samples affected:** Echo

**Fix applied:** `AppId` property added to `Context<TActivity>` delegating to `TeamsBotApplication.AppId`.

**File:** `core/src/Microsoft.Teams.Apps/Context.cs`

---

### BC-6: App.Builder() pattern removed (2 samples affected)

**Decision: IMPLEMENTED** — `App.Builder()` added as a wrapper around ASP.NET DI

**Old API:**
```csharp
var appBuilder = App.Builder()
    .AddLogger(new ConsoleLogger(...))
    .AddOAuth("graph");
builder.AddTeams(appBuilder);
```

**Samples affected:** Graph, Meetings

**Fix applied:** `AppBuilder` class added (`core/src/Microsoft.Teams.Apps/AppBuilder.cs`) wrapping `TeamsBotApplicationOptions`. `AddTeams(WebApplicationBuilder, AppBuilder)` overload added to `TeamsBotApplication.HostingExtensions.cs`.

---

### BC-7: `AddTeams()` extension target changed (ALL samples affected)

**Decision: IMPLEMENTED** — parameterless overload on `WebApplicationBuilder`

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

**Fix applied:** Parameterless `AddTeams(this WebApplicationBuilder builder)` extension method added, delegating to `builder.Services.AddTeams()`.

**File:** `core/src/Microsoft.Teams.Apps/TeamsBotApplication.HostingExtensions.cs`

---

### BC-8: Proactive messaging from app-level (1 sample affected)

**Decision: IMPLEMENTED**

**Old API:**
```csharp
await teams.Send(conversationId, "text", cancellationToken: ct);
await teams.Reply(conversationId, messageId, "text", ct);
```

**Samples affected:** Threading

**Fix applied:** `Send(string conversationId, string text, ...)` and `Reply(string conversationId, string messageId, string text, ...)` convenience methods added on `TeamsBotApplication`, delegating to `SendAsync`/`ReplyAsync`.

**File:** `core/src/Microsoft.Teams.Apps/TeamsBotApplication.cs`

---

### BC-10: Meeting handler renames (1 sample affected)

**Decision: IMPLEMENTED** — aliases without `[Obsolete]`

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

**Fix applied:** `OnMeetingJoin()` and `OnMeetingLeave()` aliases added, delegating to `OnMeetingParticipantJoin`/`OnMeetingParticipantLeave`. No `[Obsolete]` for now.

**File:** `core/src/Microsoft.Teams.Apps/Handlers/MeetingHandler.cs`

---

### BC-12: Invoke handler return types changed (4 samples affected)

**Decision: IMPLEMENTED** — factory methods only (no implicit conversions)

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

**Fix applied:** Factory methods added:
- `InvokeResponse.Ok(body)` — wraps body with 200 status
- `InvokeResponse.Error(status, body)` — wraps body with error status

**File:** `core/src/Microsoft.Teams.Apps/Handlers/InvokeHandler.Response.cs`

---

### BC-15: MessageActivity fluent methods removed

**Decision: IMPLEMENTED** — MessageActivity fluent extension methods added.

Methods added: `WithText()`, `AddText()`, `WithSuggestedActions()`, `WithTextFormat()`, `WithAttachmentLayout()`, `AddAttachment()`, `AddStreamFinal()`.

Not migrated (low priority): `WithSummary()`, `WithDeliveryMode()`, `Merge()`.
Not migrated (now deprecated in old lib — not a breaking change): `WithSpeak()`, `WithInputHint()`, `WithImportance()`, `WithExpiration()`.

---

### BC-16: `AddSensitivityLabel()` missing on TeamsActivity

**Decision: IMPLEMENTED** — Extension method added in `MessageActivityExtensions`.

---

### BC-17: Base Activity fluent `With*()` methods removed

**Decision: IMPLEMENTED (mostly)** — Base activity fluent methods added.

- **With* methods:** `WithId`, `WithChannelId`, `WithFrom`, `WithRecipient`, `WithRecipient(..., bool isTargeted)`, `WithConversation`, `WithServiceUrl`, `WithLocale`, `WithTimestamp`, `WithLocalTimestamp`, `WithData(ChannelData)`, `WithData(string, object?)`, `WithAppId`
- **Add* methods:** `AddEntity`, `UpdateEntity`, `AddAIGenerated`, `AddFeedback(bool)`, `AddTargetedMessageInfo`, `AddCitation`, `AddMention`, `AddSensitivityLabel`, `AddClientInfo`
- **Get* methods:** `GetAccountMention`

Remaining gap: `WithRelatesTo` — no longer a concern since `RelatesTo` is marked `[Obsolete("This will be removed by end of summer 2026.")]` in commit 6f33aba.

---

### BC-22: `Conversation.ToThreadedConversationId()` missing

**Decision: IMPLEMENTED** — Available as `ConversationExtensions.ToThreadedConversationId(conversationId, messageId)` in `Microsoft.Teams.Core`. Note: in the old library this was a static method on the `Conversation` class; in the new library it lives on `ConversationExtensions`. Direct callers will need a minor namespace/type adjustment, but the functionality is available and `TeamsBotApplication.Reply()` uses it internally.

**File:** `core/src/Microsoft.Teams.Core/Schema/ConversationExtensions.cs`

---

## Sample Migration Difficulty Assessment

"Remaining Blockers" lists only pending library-side work. Implemented items (BC-1, 2, 5, 6, 7, 8, 10, 12, 15, 16, 17, 22) are no longer blockers.

| Sample | Difficulty | Remaining Blockers |
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

## Verification

After each item:
1. Build `core/src/Microsoft.Teams.Apps` to verify compilation
2. Verify existing tests pass (if any)
3. For each item, attempt to mentally trace migration of affected sample code to confirm the gap is closed

After all items:
1. Migrate Samples.Echo as proof-of-concept to validate the full compat surface
2. Run the migrated sample to verify end-to-end functionality
