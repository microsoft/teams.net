# Migration from 2.0 (`release/v2.0`) to 2.1

This document describes consumer-visible breaking changes when moving from the
legacy implementation on the `release/v2.0` branch to the current implementation
in `src/`. It starts with the application-level changes and then covers detailed
API migrations.

The primary comparison is:

- `Libraries/Microsoft.Teams.Apps` on `release/v2.0` and its public types from
  `Libraries/Microsoft.Teams.Api`, `Microsoft.Teams.Common`, and
  `Microsoft.Teams.Cards`
- `src/Microsoft.Teams.Apps` and `src/Microsoft.Teams.Core`

The comparison reflects the source tree as of July 30, 2026.

## What this document counts as breaking

A change is listed as breaking only when a non-deprecated legacy API is
removed, renamed, changes its signature or behavior, or requires a different
application architecture.

The following are deliberately not counted as breaking:

- A legacy API that was already marked `[Obsolete]`.
- An API that still exists in `src/`, even if the compatibility member is now
  marked `[Obsolete]`.
- A new API or capability that did not exist in `Libraries/`.

A namespace or type move is a source-level breaking change when consumers must
change and recompile their code, even if the replacement is mechanical.

## High-level summary

The package ID `Microsoft.Teams.Apps` is unchanged, but the implementation is
not a drop-in binary replacement. The largest migration is architectural:

| Area | Legacy `release/v2.0` model | Current `src/` model | Impact |
|---|---|---|---|
| Application | `App`, `AppBuilder`, and plugins | `TeamsBotApplication`, ASP.NET Core DI, and endpoint mapping | Application startup and extension points must be migrated |
| Activities | `Microsoft.Teams.Api.Activities.IActivity` used for inbound and outbound data | `TeamsActivity` for inbound data and `TeamsActivityInput` for outbound data | Namespaces, types, and send construction change |
| Context | `IContext<T>` with sender, storage, reference, and route chaining | `Context<T>` with `Activity`, `Api`, optional `State`, and send helpers | Custom handlers and middleware must be updated |
| Routing | Handlers form an explicit `context.Next()` chain | Matching non-invoke handlers all run; invokes select one handler | Handler ordering and continuation behavior change |
| API access | `Microsoft.Teams.Api.Clients.ApiClient` with `Bots`, `Users`, and custom HTTP abstractions | `Microsoft.Teams.Apps.Clients.ApiClient` with `Conversations`, `UserToken`, `Teams`, and `Meetings` | Client construction, hierarchy, and authentication change |
| Bot authentication | Custom credentials and token acquisition owned by `App` | Microsoft.Identity.Web and MSAL token acquisition configured through standard .NET configuration and DI | Configuration moves; the standards-based auth stack is an additive 2.1 improvement |
| OAuth | App-wide settings and sign-in events | Registered `OAuthFlow` instances and per-flow callbacks | Authentication setup and callbacks must be migrated |
| Responses | General `Response` and `Response<T>` wrappers | `InvokeResponse` and feature-specific response builders | Invoke handlers must return the new response types |
| Dependencies | Apps transitively references Api, Cards, and Common | Apps references Core; Cards is separate; standard .NET replaces Common | Direct uses of old transitive packages require explicit migration |

For a typical message bot, the minimum migration is:

1. Replace `App.Builder().Build()` with
   `AddTeamsBotApplication()` and `UseTeamsBotApplication()`.
2. Move activity imports to `Microsoft.Teams.Apps` and
   `Microsoft.Teams.Apps.Schema`.
3. Change outbound `MessageActivity` construction to
   `MessageActivityInput`.
4. Update handler contexts and any code that depends on `Ref`, `Storage`,
   `Next()`, or the old API client hierarchy.

## Package and namespace changes

### Package dependency shape

The legacy `Microsoft.Teams.Apps` project referenced:

- `Microsoft.Teams.Api`
- `Microsoft.Teams.Cards`
- `Microsoft.Teams.Common`

The current `Microsoft.Teams.Apps` project references
`Microsoft.Teams.Core`. It no longer exposes the old dependency graph.

| Legacy dependency or namespace | Current destination |
|---|---|
| `Microsoft.Teams.Api.Activities` | `Microsoft.Teams.Apps` and `Microsoft.Teams.Apps.Schema` |
| Core conversation and token clients | `Microsoft.Teams.Core` |
| `Microsoft.Teams.Api.Clients` | `Microsoft.Teams.Apps.Clients` and `Microsoft.Teams.Core` |
| `Microsoft.Teams.Common.Logging` | `Microsoft.Extensions.Logging` |
| `Microsoft.Teams.Common.Http` | `System.Net.Http` and `IHttpClientFactory` |
| `Microsoft.Teams.Common.Storage` | `Context.State`, `IDistributedCache`, or an application-owned service |
| `Microsoft.Teams.Cards` | Add an explicit package reference when used; attach card payloads with `TeamsAttachment` |
| Legacy plugin assemblies | Standard ASP.NET Core middleware and dependency injection |

Code that relied on `Microsoft.Teams.Api`, `Microsoft.Teams.Cards`, or
`Microsoft.Teams.Common` only because they were transitive dependencies must
add an explicit replacement dependency or migrate to the types above.

### Activity namespace and base type

Legacy handlers use `IActivity` and activity types under
`Microsoft.Teams.Api.Activities`. Current handlers use `TeamsActivity` and
feature-specific activity types under `Microsoft.Teams.Apps` and
`Microsoft.Teams.Apps.Schema`.

```csharp
// Legacy
using Microsoft.Teams.Api.Activities;

IActivity activity = context.Activity;

// Current
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Schema;

TeamsActivity activity = context.Activity;
```

This is a source-level break. There is no `IActivity` compatibility interface
in the current Apps package.

## Application startup and hosting

### `App` is replaced by `TeamsBotApplication`

Most legacy `AppBuilder` methods do not exist in the current implementation.

```csharp
// Legacy
var teams = App.Builder()
    .AddLogger()
    .AddStorage(storage)
    .AddPlugin(plugin)
    .AddOAuth("graph")
    .Build();

builder.AddTeams(teams);
```

```csharp
// Current
builder.Services.AddTeamsBotApplication(options =>
{
    options.UseState();
    options.AddOAuthFlow("graph");
});

builder.Services.AddSingleton<MyService>();

var app = builder.Build();
TeamsBotApplication teams = app.UseTeamsBotApplication();
```

`App.Builder().AddOAuth().Build()` and the `AddTeams()`/`UseTeams()` hosting
names still exist as deprecated compatibility shims. They are therefore not
breaking changes by themselves. Other active legacy builder methods require
migration:

| Legacy builder method | Current replacement |
|---|---|
| `AddLogger(...)` | `TeamsBotApplication` uses the `ILogger` infrastructure registered in DI; configure providers through `builder.Logging` |
| `AddStorage(...)` | Call `options.UseState()` and register an `IDistributedCache` implementation when the in-memory default is insufficient |
| `AddClient(...)` | Register clients through `builder.Services.AddHttpClient(...)` and use `IHttpClientFactory` |
| `AddCredentials(...)` | Configure the Microsoft.Identity.Web/MSAL identity under `AzureAd` |
| `AddPlugin(...)` | Register application services through DI; use ASP.NET Core middleware or `ITurnMiddleware` for pipeline behavior |
| `AddCloud(...)` | Configure Entra and Bot Framework cloud endpoints through `AzureAd` and `BotFramework` configuration |
| `AutoUserTokenLookup(...)` | Register an `OAuthFlow` and request or check tokens explicitly for the required connection |

### Plugin architecture is removed

The active legacy `IPlugin` and `ISenderPlugin` extension model has no current
equivalent. This includes plugin initialization, startup, activity, response,
sent-activity, and error callbacks.

| Legacy extension point | Current replacement |
|---|---|
| `IPlugin.OnInit` / `OnStart` | ASP.NET Core service registration and hosted services |
| `IPlugin.OnActivity` | `ITurnMiddleware` or typed handlers |
| `IPlugin.OnActivitySent` | Wrap the relevant client/send operation or instrument it |
| `IPlugin.OnActivityResponse` | Invoke handler response logic or ASP.NET Core middleware |
| `IPlugin.OnError` / `app.OnError` | Exception middleware, logging, and `BotHandlerException` handling |
| `ISenderPlugin` | Core `ConversationClient` and Apps `ApiClient` |

## Routing and middleware

### Route continuation behavior changed

Legacy routes form a chain. A handler must call `context.Next()` to run the
next matching route. Current non-invoke routing finds every matching route and
runs them sequentially; `Context<T>` has no `Next()` method.

This affects applications that use a broad handler before a narrow handler,
for example a general `OnMessage` handler followed by a regex-specific
`OnMessage` handler. In the current router, both handlers run when both match.

Invoke routing behaves differently again:

- Only the first matching invoke route runs.
- `OnInvoke` cannot be registered together with specific invoke handlers.
- Registering overlapping catch-all and specific invoke routes throws
  `InvalidOperationException`.

Review handler ordering and remove explicit `context.Next()` calls during
migration.

### Middleware receives Core types

The legacy middleware shape:

```csharp
teams.Use(async context =>
{
    // IContext<IActivity>
    await context.Next();
});
```

The current middleware shape:

```csharp
public sealed class MyMiddleware : ITurnMiddleware
{
    public async Task OnTurnAsync(
        BotApplication bot,
        CoreActivity activity,
        NextTurn next,
        CancellationToken cancellationToken)
    {
        await next(cancellationToken);
    }
}

teams.UseMiddleware(new MyMiddleware());
```

Current middleware runs before Teams activity conversion and typed routing. It
receives `CoreActivity`, not `Context<TeamsActivity>`.

### Handler migrations

Most common typed handler names remain available, including `OnMessage`,
`OnAdaptiveCardAction`, `OnQuery`, `OnSubmitAction`, `OnQueryLink`,
`OnFetchTask`, `OnTaskFetch`, `OnTaskSubmit`, conversation updates, meetings,
installs, reactions, and file consent.

The following active legacy handlers require a rename or consolidation:

| Legacy handler | Current handler |
|---|---|
| `OnFeedback` | `OnMessageSubmitFeedback` |
| `OnQuerySettingsUrl` | `OnQuerySettingUrl` |
| `OnTypeaheadSearch` | `OnSearch` |
| `OnAnswerSearch` | `OnSearch` |
| `OnSignIn` | `OAuthFlow.OnSignInComplete` |
| `OnSignInFailure` | `OAuthFlow.OnSignInFailure` |
| `OnTokenExchange` | Managed by the registered `OAuthFlow` |
| `OnVerifyState` | Managed by the registered `OAuthFlow` |

The following active legacy handlers have no dedicated current equivalent:

- Generic `OnActivity` overloads and predicate routes
- `OnTyping`
- `OnCommand` and `OnCommandResult`
- `OnTabFetch` and `OnTabSubmit`
- `OnConfigFetch` and `OnConfigSubmit`
- `OnExecuteAction`
- `OnHandoff`
- `OnReadReceipt`
- `OnTeamHardDeleted`
- App lifecycle callbacks such as `OnStart`, `OnActivitySent`,
  `OnActivityResponse`, and `OnError`

Some raw activity types can be handled in Core middleware, but that is not
source-compatible with the old typed handler and context contract.

## Context changes

`IContext<TActivity>` is replaced by the concrete `Context<TActivity>`.
Handler delegates must use the current context type.

The following properties remain available:

- `Activity`
- `Api`
- `AppId`
- `Log` as a deprecated compatibility shim

The following active legacy context members require migration:

| Legacy member | Current approach |
|---|---|
| `TenantId` | Read `Activity.Conversation?.TenantId` or `Activity.ChannelData?.Tenant?.Id` |
| `Storage` | Enable `options.UseState()` and use `context.State`, or inject application-owned storage |
| `Ref` | Read routing data from `Activity.Conversation`, `Activity.ServiceUrl`, `Activity.From`, and `Activity.Recipient` |
| `UserGraphToken` | Use the registered `OAuthFlow` to acquire the token |
| `Extra` | Use typed DI services or turn state |
| `CancellationToken` | Use the token passed to the handler |
| `Sender` | Use `context.Api`, `ConversationClient`, or send helpers |
| `Next()` | Remove it and account for current routing behavior |
| `ToActivityType<T>()` | Register a typed handler; derived contexts are created by the router |
| Context deconstruction | Read the required properties explicitly |
| `IContext.Client` | Call `SendAsync`, `ReplyAsync`, or `TypingAsync` directly |

### Send and reply return types

The deprecated `Send`, `Reply`, `Typing`, and `Quote` names still exist for
common cases, so the name change to their `Async` forms is not itself a break.
Their contract is not fully compatible:

- Legacy sends return the sent activity. Current sends return
  `SendActivityResponse?`.
- Legacy generic methods accept any `IActivity`. Current helpers use
  `MessageActivityInput` or the deprecated inbound `MessageActivity` overload.
- Legacy `Typing(string? text, ...)` accepts optional text. Current
  `TypingAsync(...)` sends a typing indicator without a text argument.
- Legacy overloads accept `Microsoft.Teams.Cards.AdaptiveCard` directly.
  Current code must create a `TeamsAttachment` and add it to a
  `MessageActivityInput`.
- Proactive legacy methods accept `ConversationType?` and a string service
  URL. Current methods accept a `Uri?` service URL and no conversation-type
  argument.

Code that only awaits `context.Send("text", ct)` continues through the
compatibility shim after namespace migration. Code that consumes its return
value or uses the generic, card, typing-text, or positional proactive
overloads must change.

## Incoming and outgoing activity models

### Separate outbound input types

The legacy SDK uses mutable activity types for both received and sent data. The
current SDK separates them:

- `MessageActivity` and other `TeamsActivity` types represent received data.
- `MessageActivityInput` and `TeamsActivityInput` represent data to send.
- Conversation ID and service URL are transport arguments rather than fields
  copied onto the outbound activity.

```csharp
// Legacy
var message = new MessageActivity("Hello")
    .WithConversation(conversation)
    .WithServiceUrl(serviceUrl);

await context.Send(message, cancellationToken);
```

```csharp
// Current
var message = new MessageActivityInput()
    .WithText("Hello");

await context.SendAsync(message, cancellationToken);
```

Constructors and fluent methods on the current inbound `MessageActivity` still
exist as deprecated compatibility members. Their presence means those members
are not classified as removed, but new code should use the input model.

### Typed property changes

| Property or type | Legacy | Current |
|---|---|---|
| Activity base | `IActivity` / `Activity` | `CoreActivity` / `TeamsActivity` |
| Account | `Microsoft.Teams.Api.Account` | `ChannelAccount` or `TeamsChannelAccount` |
| `ServiceUrl` | `string?` | `Uri?` |
| `Timestamp` / `LocalTimestamp` | `DateTime?` | Protocol-formatted `string?` |
| Attachments | `Microsoft.Teams.Api.Attachment` | `TeamsAttachment` |
| Invoke value | Many dedicated legacy activity subclasses | `InvokeActivity<TValue>` and current feature value types |

The active legacy `MessageActivity.Summary`, `DeliveryMode`, and `Value`
properties do not have typed properties on the current `MessageActivity` or
`MessageActivityInput`. Inbound extension data remains available through the
activity property bag, and custom outbound fields can be added through the
input builder/property bag when the service supports them.

`SuggestedActions.AddRecipients`, `AddAction`, and `AddActions` are present in
the current schema. They are not breaking changes.

## API client changes

The legacy `Microsoft.Teams.Api.Clients.ApiClient` can be directly constructed
with a service URL and exposes:

- `Bots`
- `Conversations`
- `Users`
- `Teams`
- `Meetings`
- the custom `Microsoft.Teams.Common.Http.IHttpClient`

The current `Microsoft.Teams.Apps.Clients.ApiClient` is created by DI and
exposes:

- `Conversations`
- `UserToken`
- `Teams`
- `Meetings`

Use `context.Api` inside a handler. It is scoped from the inbound activity's
service URL and agentic identity. Outside a handler, resolve the client and use
`ForServiceUrl`, `ForAgenticIdentity`, or `ForActivity` as appropriate.

Bot token acquisition and HTTP credentials are handled by the Core hosting and
authentication pipeline; there is no current `Api.Bots` hierarchy. Legacy
`Api.Users.Token` operations move to `Api.UserToken`. Conversation member,
reaction, and activity methods are consolidated under
`Api.Conversations`.

Client method names, request models, return models, and the `ServiceUrl` type
are not source-compatible. Migrate client calls individually rather than
performing only a namespace replacement.

## Authentication

### Bot authentication

Applications that directly use the active 2.0 `AddCredentials`, custom HTTP
credentials, `Api.Bots.Token`, or manual token lifecycle APIs must migrate
those call sites to the Microsoft.Identity.Web/MSAL authentication pipeline
registered by `AddBotApplication()` or `AddTeamsBotApplication()`. Bot identity
and credentials are supplied through the Microsoft.Identity.Web-compatible
`AzureAd` configuration section and standard dependency injection.

The legacy `Teams` configuration section is still mapped to `AzureAd` by a
compatibility path, so the section rename alone is not a breaking change.

### User OAuth and SSO

Legacy OAuth is app-wide:

```csharp
var teams = App.Builder()
    .AddOAuth("graph")
    .Build();

teams.OnSignIn(async (_, signInEvent, ct) => { });
```

Current OAuth is registered and handled per connection:

```csharp
builder.Services.AddTeamsBotApplication(options =>
    options.AddOAuthFlow("graph"));

TeamsBotApplication teams = app.UseTeamsBotApplication();
OAuthFlow graph = teams.GetOAuthFlow("graph");

graph.OnSignInComplete = async (context, token, ct) => { };
graph.OnSignInFailure = async (context, ct) => { };
```

The compatibility `App.Builder().AddOAuth()` path still exists and is not
counted as a break. The callback model, `AutoUserTokenLookup`, context token
properties, and direct old token-client usage are breaking changes.

Bot credentials and cloud settings move from `AppBuilder.AddCredentials()` and
`AddCloud()` to the `AzureAd` configuration consumed by
`AddTeamsBotApplication()`.

## Invoke response changes

Legacy invoke handlers may return `Response`, `Response<T>`, or legacy feature
response models. Current invoke handlers return `InvokeResponse` or a
feature-specific response produced by builders such as:

- `AdaptiveCardResponse`
- `MessageExtensionResponse`
- `TaskModuleResponse`

```csharp
// Legacy
return new Response<MyBody>(HttpStatusCode.OK, body);

// Current
return InvokeResponse.Ok(body);
```

The general legacy `Response` metadata (`Routes` and `Elapse`) is not part of
the current invoke response contract.

## Existing compatibility surface: not breaking

The following compatibility members exist in `src/`. They may produce
deprecation warnings, but code is not forced to migrate immediately:

| Legacy pattern | Current compatibility |
|---|---|
| `builder.AddTeams()` | Deprecated shim to `AddTeamsBotApplication()` |
| `app.UseTeams()` | Deprecated shim to `UseTeamsBotApplication()` |
| `App.Builder().AddOAuth(...).Build()` | Deprecated OAuth-only builder shim accepted by `AddTeams(builder)` |
| `context.Log.Info/Error/Debug/Warn` | Deprecated `ContextLogger` shim |
| `context.Send(...)` | Deprecated overloads for text and `MessageActivity` |
| `context.Reply(...)` | Deprecated overloads for text and `MessageActivity` |
| `context.Typing()` | Deprecated alias |
| `context.Quote(...)` | Deprecated overloads for text and `MessageActivity` |
| `teams.Send(...)` / `teams.Reply(...)` | Deprecated proactive aliases |
| `new MessageActivity(...)` and its fluent methods | Deprecated inbound-model compatibility surface |
| `InvokeResponse.Ok(...)` / `Error(...)` | Available |
| `SuggestedActions.AddRecipients/AddAction/AddActions` | Available |
| `OnSetting` and `OnCardButtonClicked` | Available |

These shims should still be migrated before their announced removal, but their
deprecation is not a breaking change in the current release.

## Deprecated legacy APIs excluded from the break list

Examples of legacy APIs intentionally excluded because they were already
marked `[Obsolete]` in `Libraries/` include:

- `AddController<T>()` and controller annotations
- `Activity.RelatesTo`, `WithRelatesTo`, and `ToQuoteReply()`
- `EndOfConversationActivity` and `EndOfConversationCode`
- `Account.Role`
- `Attachment.Id`
- `MessageActivity.Speak`, `InputHint`, `Importance`, and `Expiration`, plus
  their fluent setters
- Message-reaction mutation helpers that direct callers to the reactions
  client
- Unpaged member APIs where the legacy SDK directs callers to paged methods
- Handler overloads without a cancellation-token parameter when the legacy SDK
  already directs callers to the cancellation-token overload

Their absence or replacement in `src/` is cleanup of an already-deprecated
surface, not a new migration break.

## Additions in 2.1: not breaking

The following capabilities are additive and must not be presented as breaking
changes:

- The new `Microsoft.Teams.Core` foundation package.
- .NET 10 support in addition to .NET 8.
- Microsoft.Identity.Web/MSAL application authentication, distributed token
  caching, managed and federated credentials, and agent identity support.
- Per-turn conversation and user state backed by `IDistributedCache`.
- OpenTelemetry spans and metrics and Agent 365 baggage propagation.
- Agentic identity and agent lifecycle support.
- Targeted messages and prompt-preview metadata.
- HTML widget invoke handlers.
- `MessageActivityInput` and the explicit outbound activity model.
- `TeamsStreamingWriter`.
- Proactive `SendAsync` and `ReplyAsync` helpers with service URL caching.
- New or newly completed handlers such as `OnSetting`,
  `OnCardButtonClicked`, agent lifecycle handlers, and widget tool calls.

## Recommended migration order

1. Replace startup and plugin composition with ASP.NET Core DI and
   `TeamsBotApplication`.
2. Move namespaces and compile before changing behavior.
3. Convert outbound activities to input types.
4. Migrate context members and API client calls.
5. Rewrite middleware and audit route ordering for the new dispatch behavior.
6. Migrate OAuth registration and callbacks.
7. Replace invoke response types.
8. Handle any unsupported typed handlers at the Core middleware or raw
   protocol layer.
9. Remove use of deprecated compatibility shims after behavior is verified.
