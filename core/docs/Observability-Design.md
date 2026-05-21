# Observability Design

## Overview

The Teams .NET SDK (`Microsoft.Teams.Core`, `Microsoft.Teams.Apps`, `Microsoft.Teams.Apps.BotBuilder`) emits OpenTelemetry-compatible traces, metrics, and logs so that consuming bots can wire observability through the [Microsoft OpenTelemetry distro](https://github.com/microsoft/opentelemetry-distro-dotnet) and ship telemetry to Azure Monitor, an OTLP collector (Aspire Dashboard, Grafana LGTM, Jaeger), or the console.

The SDK uses the BCL `System.Diagnostics.ActivitySource` and `System.Diagnostics.Metrics.Meter` for the trace and metric APIs. The OpenTelemetry SDK and exporters are an application concern: the bot project references `Microsoft.OpenTelemetry`, subscribes to the SDK's source/meter by name, and configures exporters. `Microsoft.Teams.Core` takes a single new package dependency on `OpenTelemetry.Api` so the `CoreBaggageBuilder` can write to `OpenTelemetry.Baggage.Current` (see "Dependency impact" below).

```
Consuming bot                            Teams SDK (this design)
─────────────                            ───────────────────────
.UseMicrosoftOpenTelemetry(...)
                                         ActivitySource("Microsoft.Teams.Core")
.WithTracing(t => t                       ├─ "turn"                (BotApplication.ProcessAsync)
    .AddSource(CoreTelemetryNames         ├─ "middleware"          (TurnMiddleware.RunPipelineAsync)
        .ActivitySourceName)              ├─ "auth.outbound"       (BotAuthenticationHandler)
    .AddSource(                           └─ "conversation_client" (ConversationClient send/update/delete)
        TeamsBotApplicationTelemetry
        .ActivitySourceName))            ActivitySource("Microsoft.Teams.Apps")
                                          └─ "handler"             (Router.DispatchAsync)
.WithMetrics(m => m
    .AddMeter(CoreTelemetryNames         Meter("Microsoft.Teams.Core")
        .MeterName)                       ├─ teams.activities.received   (Counter)
    .AddMeter(                            ├─ teams.turn.duration         (Histogram, ms)
        TeamsBotApplicationTelemetry      ├─ teams.handler.errors        (Counter)
        .MeterName));                     ├─ teams.middleware.duration   (Histogram, ms)
                                          ├─ teams.outbound.calls        (Counter)
                                          └─ teams.outbound.errors       (Counter)

                                         Meter("Microsoft.Teams.Apps")
                                          ├─ teams.handler.dispatched    (Counter)
                                          ├─ teams.handler.duration      (Histogram, ms)
                                          ├─ teams.handler.failures      (Counter)
                                          └─ teams.handler.unmatched     (Counter)
```

## Layering constraints

The SDK is split across two assemblies that observability must respect:

- `Microsoft.Teams.Core` is the lower layer. It owns `BotApplication`, the turn pipeline (`TurnMiddleware`), the outbound HTTP clients (`ConversationClient`, `UserTokenClient`), and the auth-handler (`BotAuthenticationHandler`). It must **not reference anything in `Microsoft.Teams.Apps`**, including no string literals or constants tied to the Apps brand.
- `Microsoft.Teams.Apps` depends on Core. It owns the typed activity model, `TeamsBotApplication`, and the `Router` that dispatches to user handlers.

Telemetry follows the same rule: **each assembly publishes its own ActivitySource and Meter, named after the assembly.** A class named `TeamsBotApplicationTelemetry` describes Apps-level telemetry; it lives in Apps. Core's analogue is `CoreTelemetryNames`. Neither references the other.

| Layer | Public name class | Source / Meter name | Spans | Metrics |
|---|---|---|---|---|
| `Microsoft.Teams.Core` | `Microsoft.Teams.Core.Diagnostics.CoreTelemetryNames` | `"Microsoft.Teams.Core"` | `turn`, `middleware`, `auth.outbound`, `conversation_client` | `teams.activities.received`, `teams.turn.duration`, `teams.handler.errors`, `teams.middleware.duration`, `teams.outbound.calls`, `teams.outbound.errors` |
| `Microsoft.Teams.Apps` | `Microsoft.Teams.Apps.Diagnostics.TeamsBotApplicationTelemetry` | `"Microsoft.Teams.Apps"` | `handler` | `teams.handler.dispatched`, `teams.handler.duration`, `teams.handler.failures`, `teams.handler.unmatched` |

Cross-assembly use is one-way: Apps's `Router` may call Core utilities (for example, the public `RecordException` extension on `Activity` defined in `Microsoft.Teams.Core.Diagnostics.ActivityExtensions`), but Core never reaches up into Apps. If a future Core-level helper would need an Apps concept, that helper belongs in Apps, not in Core.

A consumer that uses both layers (the common case) registers both names. A consumer that only references Core (a minimal `BotApplication` bot without the `TeamsBotApplication` router) registers just `CoreTelemetryNames` and gets the full Core-level signal.

## Public surface

```csharp
namespace Microsoft.Teams.Core.Diagnostics;
public static class CoreTelemetryNames
{
    public const string ActivitySourceName = "Microsoft.Teams.Core";
    public const string MeterName          = "Microsoft.Teams.Core";
}

namespace Microsoft.Teams.Apps.Diagnostics;
public static class TeamsBotApplicationTelemetry
{
    public const string ActivitySourceName = "Microsoft.Teams.Apps";
    public const string MeterName          = "Microsoft.Teams.Apps";
}
```

The matching internal singletons live in each assembly's `Diagnostics/` folder:
- `Microsoft.Teams.Core/Diagnostics/Telemetry.cs` — owned by Core; internal to `Microsoft.Teams.Core` (Apps has its own `AppsTelemetry` class).
- `Microsoft.Teams.Apps/Diagnostics/AppsTelemetry.cs` — owned by Apps; the class is named `AppsTelemetry` to avoid collision with the Core `Telemetry` class when both namespaces are imported.

## Spans

The auto-instrumented HTTP-server span (from the OTel distro's ASP.NET Core instrumentation) is the parent of `turn`. Outbound HTTP-client spans (from the distro's HttpClient instrumentation) are children of `auth.outbound` and `conversation_client` automatically because the SDK opens the span before the underlying HTTP call. The `handler` span (from Apps) nests inside `turn` (from Core) via the ambient `Activity.Current`, even though the two spans come from different sources.

| Span | Source | Where | Tags |
|---|---|---|---|
| `turn` | Core | `Microsoft.Teams.Core/BotApplication.cs` `ProcessAsync` body, after the request body has been deserialized into a `CoreActivity` | `activity.type`, `activity.id`, `conversation.id`, `channel.id`, `bot.id`, `service.url` |
| `middleware` | Core | `Microsoft.Teams.Core/TurnMiddleware.cs` `RunPipelineAsync` per-middleware execution | `middleware.name`, `middleware.index` |
| `handler` | Apps | `Microsoft.Teams.Apps/Routing/Router.cs` `DispatchAsync` matched-route invocation | `handler.type` (activity type or invoke name), `handler.dispatch` (`type` / `invoke` / `catchall`) |
| `auth.outbound` | Core | `Microsoft.Teams.Core/Hosting/BotAuthenticationHandler.cs` `GetAuthorizationHeaderAsync` | `auth.flow` (`agentic` / `app_only` / `managed_identity`) |
| `conversation_client` | Core | `Microsoft.Teams.Core/ConversationClient.cs` `SendActivityAsync` / `UpdateActivityAsync` / `DeleteActivityAsync` | `service.url`, `conversation.id`, `activity.type`, `activity.id` (set after response when known), `operation` |

On exception every span sets `Status = Error` with the exception message and adds an `exception` span event with `exception.type`, `exception.message`, and `exception.stacktrace` tags. This is done through the `RecordException` extension method in `Microsoft.Teams.Core.Diagnostics.ActivityExtensions`, which is `public` so the Apps layer (`Router`) can use it too. The extension uses manual event tagging on both `net8.0` and `net10.0` to stay consistent across target frameworks; it intentionally does not delegate to the BCL `Activity.AddException` (added in .NET 9), because that API only adds the event without setting `ActivityStatusCode.Error`.

### `auth.inbound` is intentionally omitted

The `auth.inbound` span belongs to the auth middleware, not the bot pipeline. The SDK uses `Microsoft.AspNetCore.Authentication.JwtBearer` for inbound auth, which is already covered by the OTel distro's ASP.NET Core HTTP-server instrumentation. Adding a separate inbound-auth span would duplicate signal without new information; it is out of scope for this design.

## Metrics

Core-meter instruments cover the turn pipeline, middleware, and outbound HTTP clients. Apps-meter instruments cover router dispatch (one observation per matched route).

### Core meter (`Microsoft.Teams.Core`)

| Metric | Kind | Unit | Tags | Where |
|---|---|---|---|---|
| `teams.activities.received` | Counter | — | `activity.type` | top of `BotApplication.ProcessAsync` |
| `teams.turn.duration` | Histogram | ms | `activity.type` | `finally` of the `turn` span |
| `teams.handler.errors` | Counter | — | `activity.type` | catch block in `BotApplication.ProcessAsync` |
| `teams.middleware.duration` | Histogram | ms | `middleware.name` | `finally` of the `middleware` span |
| `teams.outbound.calls` | Counter | — | `operation` ∈ {`sendActivity`, `updateActivity`, `deleteActivity`} | success branch of `ConversationClient` calls |
| `teams.outbound.errors` | Counter | — | `operation` | exception branch of `ConversationClient` calls |

### Apps meter (`Microsoft.Teams.Apps`)

| Metric | Kind | Unit | Tags | Where |
|---|---|---|---|---|
| `teams.handler.dispatched` | Counter | — | `handler.type`, `handler.dispatch` | `Router.DispatchAsync` / `DispatchWithReturnAsync` before each matched-route invocation |
| `teams.handler.duration` | Histogram | ms | `handler.type`, `handler.dispatch` | `finally` block around each matched-route invocation (recorded even on exception) |
| `teams.handler.failures` | Counter | — | `handler.type`, `handler.dispatch` | catch block when a route handler throws |
| `teams.handler.unmatched` | Counter | — | `activity.type` (DispatchAsync) or `activity.type` + `invoke.name` (DispatchWithReturnAsync) | branch where no route selector matched |

OTLP exposes these names with dots; Prometheus/Mimir maps them to `teams_*_total` (counters) and `teams_*_milliseconds_*` (histograms).

## Logs

The OTel distro's `UseMicrosoftOpenTelemetry()` automatically wires `ILogger` to OTel log records and stamps every record with the active `Activity` trace and span IDs. Existing `BotApplication._logger.BeginActivityScope(...)` already adds `ActivityType` / `ActivityId` / `ServiceUrl` / `MSCV` to the scope dictionary, so those fields ride along on every log record produced inside a turn. **No SDK changes are required for logs.**

The TODO at `Microsoft.Teams.Core/BotApplication.cs:202` (`// TODO: Replace with structured scope data, ensure it works with OpenTelemetry...`) is resolved by this design and is removed.

## Consumer integration

```csharp
using Microsoft.OpenTelemetry;
using Microsoft.Teams.Apps.Diagnostics;
using Microsoft.Teams.Core.Diagnostics;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
    .UseMicrosoftOpenTelemetry(o => o.Exporters = ExportTarget.AzureMonitor | ExportTarget.Otlp)
    .WithTracing(t => t
        .AddSource(CoreTelemetryNames.ActivitySourceName)
        .AddSource(TeamsBotApplicationTelemetry.ActivitySourceName))
    .WithMetrics(m => m
        .AddMeter(CoreTelemetryNames.MeterName)
        .AddMeter(TeamsBotApplicationTelemetry.MeterName));

builder.Logging.AddOpenTelemetry(o => o.IncludeFormattedMessage = true);
```

Standard OpenTelemetry environment variables (`OTEL_SERVICE_NAME`, `OTEL_RESOURCE_ATTRIBUTES`, `OTEL_EXPORTER_OTLP_ENDPOINT`, `APPLICATIONINSIGHTS_CONNECTION_STRING`, `OTEL_TRACES_SAMPLER`, `OTEL_TRACES_SAMPLER_ARG`) are honored by the distro without any SDK code.

A working sample lives at `core/samples/ObservabilityBot/` with a README that documents the local Grafana LGTM container loop.

## Span tree per turn

```
HTTP server span                       (auto, OTel ASP.NET Core)
└─ turn                                (Microsoft.Teams.Core)
   ├─ middleware [n times]             (Microsoft.Teams.Core)
   ├─ handler                          (Microsoft.Teams.Apps)
   └─ conversation_client              (Microsoft.Teams.Core)
      ├─ auth.outbound                 (Microsoft.Teams.Core)
      │  └─ HTTP client span           (auto, OTel HttpClient — token endpoint)
      └─ HTTP client span              (auto, OTel HttpClient — Bot Service API)
```

## Agent365 baggage and the TurnContext mismatch

When the consuming bot also exports to **Agent365** (`ExportTarget.Agent365` in the Microsoft OpenTelemetry distro), the Agent365 SDK certifies on a fixed set of OpenTelemetry baggage entries that decorate every span emitted from a turn. The distro ships three helpers in `Microsoft.Agents.A365.Observability.Hosting.Extensions` that pull these from a turn context — `BaggageBuilderExtensions.FromTurnContext(ITurnContext)`, `InvokeAgentScopeExtensions.FromTurnContext(ITurnContext)`, and `TurnContextExtensions.InjectObservabilityContext(ITurnContext, OpenTelemetryScope)`.

**These helpers take `Microsoft.Agents.Builder.ITurnContext`. The Teams SDK does not produce an `ITurnContext`** — the Apps layer hands handlers a `Microsoft.Teams.Apps.Context<TeamsActivity>`, and the Core layer has no per-turn context type at all. The two activity object models are also subtly different (see field map below). A consumer cannot pass a Teams context into `FromTurnContext(...)` directly.

We deliberately do **not** wrap `ITurnContext`: synthesizing a fake Activity shape (with `ChannelId.Channel` / `ChannelId.SubChannel` sub-properties, `StackState` dictionary, `ServiceUrl` as string) drags in `Microsoft.Agents.Builder` and is brittle to upstream changes. Instead, each Teams SDK layer ships its own baggage builder (`CoreBaggageBuilder` / `TeamsBaggageBuilder`) that reads directly off Teams types. See "Bridging strategy" below.

### Agent365 certification — crisp definition

Authoritative source: `https://github.com/microsoft/opentelemetry-distro-dotnet/blob/main/docs/agent365-getting-started.md` § "Validate for store publishing".

Two requirements gate Agent365 store publishing:

#### (1) Scope coverage

The agent **must implement** the following scopes via the Agent365 SDK (`Microsoft.Agents.A365.Observability.Runtime.Tracing.Scopes`):

| Scope | When to start | Required for publishing? |
|---|---|---|
| `InvokeAgentScope` | Top of agent processing | **Yes** |
| `InferenceScope` | Around each LLM call | **Yes** |
| `ExecuteToolScope` | Around each tool / function call | **Yes** |
| `OutputScope` | Optional — for async output capture | No |

Auto-instrumentation (Semantic Kernel, OpenAI, Azure OpenAI, Agent Framework) emits **inference** spans automatically, but `InvokeAgentScope` and `ExecuteToolScope` must be started by the agent.

#### (2) Attribute coverage

Every Required attribute on each scope must be **non-null at scope close**. The bulk of these come from baggage (set once per turn via `CoreBaggageBuilder` / `TeamsBaggageBuilder`); a handful are scope-specific and come from `ScopeDetails` / `Record*` methods.

**Common Required attributes (all scopes):**

| Key | Where it comes from |
|---|---|
| `microsoft.tenant.id` | `CoreBaggageBuilder.TenantId(...)` / `TeamsBaggageBuilder.TenantId(...)` |
| `gen_ai.agent.id` | `CoreBaggageBuilder.AgentId(...)` / `TeamsBaggageBuilder.AgentId(...)` |
| `gen_ai.agent.name` | `CoreBaggageBuilder.AgentName(...)` / `TeamsBaggageBuilder.AgentName(...)` |
| `microsoft.a365.agent.blueprint.id` | `CoreBaggageBuilder.AgentBlueprintId(...)` / `TeamsBaggageBuilder.AgentBlueprintId(...)` |
| `microsoft.agent.user.id` | `CoreBaggageBuilder.AgenticUserId(...)` / `TeamsBaggageBuilder.AgenticUserId(...)` |
| `microsoft.agent.user.email` | `TeamsBaggageBuilder.AgenticUserEmail(...)` |
| `client.address` | Caller-supplied (HTTP request remote IP) |
| `user.id` | `TeamsBaggageBuilder.UserId(...)` |
| `user.email` | `TeamsBaggageBuilder.UserEmail(...)` |
| `microsoft.channel.name` | `CoreBaggageBuilder.ChannelName(...)` / `TeamsBaggageBuilder.ChannelName(...)` |
| `gen_ai.conversation.id` | `CoreBaggageBuilder.ConversationId(...)` / `TeamsBaggageBuilder.ConversationId(...)` |
| `gen_ai.operation.name` | Set by the scope automatically |

**Scope-specific Required attributes:**

| Scope | Additional Required attributes | Source |
|---|---|---|
| `InvokeAgentScope` | `gen_ai.input.messages`, `gen_ai.output.messages`, `server.address`, `server.port` | `scope.RecordInputMessages(...)` / `RecordOutputMessages(...)` + `CoreBaggageBuilder.InvokeAgentServer(host, port)` |
| `ExecuteToolScope` | `gen_ai.tool.call.arguments`, `gen_ai.tool.call.id`, `gen_ai.tool.call.result`, `gen_ai.tool.name`, `gen_ai.tool.type` | `ToolCallDetails` + `scope.RecordResponse(...)` |
| `InferenceScope` | `gen_ai.input.messages`, `gen_ai.output.messages`, `gen_ai.provider.name`, `gen_ai.request.model` | `InferenceCallDetails` + `RecordInputMessages` / `RecordOutputMessages` |
| `OutputScope` | `gen_ai.output.messages` | `Response` constructor |

**Optional (recommended but not gating):** `gen_ai.agent.description`, `gen_ai.agent.version`, `microsoft.a365.agent.platform.id`, `microsoft.session.id`, `microsoft.session.description`, `microsoft.conversation.item.link`, `microsoft.channel.link`, all `microsoft.a365.caller.agent.*`, `microsoft.a365.agent.thought.process` (InferenceScope only).

**Out of scope of this SDK:** the scope objects themselves (`InvokeAgentScope`, `InferenceScope`, `ExecuteToolScope`, `OutputScope`) ship in `Microsoft.OpenTelemetry`. The Teams SDK only ships the `CoreBaggageBuilder` / `TeamsBaggageBuilder` that populates the cert-required baggage; agents create the scopes themselves at the appropriate boundaries.

### Required baggage map (Teams activity → Agent365 keys)

| Group | Key (Agent365 wire) | Required for cert? | Source field on the Teams activity |
|---|---|---|---|
| Tenant | `microsoft.tenant.id` | **Yes** | `Activity.Recipient.TenantId` (typed on Core's `ConversationAccount` — see "Schema change" below); fallback `Activity.ChannelData.tenant.id` (typed on Apps's `TeamsChannelData`, JSON-parsed on Core) |
| Conversation | `gen_ai.conversation.id` | **Yes** | `Activity.Conversation.Id` |
| Conversation | `microsoft.conversation.item.link` | Optional | `Activity.ServiceUrl?.ToString()` |
| Channel | `microsoft.channel.name` | **Yes** | `Activity.ChannelId` (the whole string — `"msteams"`, `"webchat"`, …) |
| Channel | `microsoft.channel.link` | Optional | No equivalent on the Teams activity — see "Channel / SubChannel mapping" below |
| Caller (human) | `user.id` | **Yes** | `((TeamsConversationAccount)Activity.From).AadObjectId` (Apps-only) |
| Caller (human) | `user.name` | Optional | `Activity.From.Name` |
| Caller (human) | `user.email` | **Yes** | `((TeamsConversationAccount)Activity.From).Email` (Apps-only) |
| Target agent | `gen_ai.agent.id` | **Yes** | `Activity.Recipient.AgenticAppId ?? Activity.Recipient.Id` |
| Target agent | `gen_ai.agent.name` | **Yes** | `Activity.Recipient.Name` |
| Target agent | `microsoft.agent.user.id` | **Yes** | `Activity.Recipient.AgenticUserId` |
| Target agent | `microsoft.agent.user.email` | **Yes** | `((TeamsConversationAccount)Activity.Recipient).Email` (Apps-only) |
| Target agent | `gen_ai.agent.description` | Optional | `((TeamsConversationAccount)Activity.Recipient).UserRole` (Apps-only) |
| Target agent | `microsoft.a365.agent.blueprint.id` | **Yes** | `Activity.Recipient.AgenticAppBlueprintId` |
| Operation source | `service.name` (set via `CoreBaggageBuilder.OperationSource` / `TeamsBaggageBuilder.OperationSource`) | **Yes** (server spans) | Caller-supplied constant (e.g. `"teams-bot"`) |

The fields the Agent365 helpers also access that have **no Teams equivalent**:

- `turnContext.StackState[O11ySpanId / O11yTraceId]` — Teams's `Context<TActivity>` has no `StackState` dictionary. Reading the active span/trace id later in the turn must go through `Activity.Current?.SpanId` / `Activity.Current?.TraceId` instead. `InjectObservabilityContext` is therefore not portable as-is.

### Channel / SubChannel mapping

The upstream `BaggageBuilderExtensions.FromTurnContext` (in Agent Builder) reads `Activity.ChannelId.Channel` and `Activity.ChannelId.SubChannel` — Agent Builder's `ChannelId` is a complex object. Teams's `ChannelId` is a **plain string** (`"msteams"`, `"webchat"`, …) and has no `SubChannel` concept. Resolution:

| Agent365 baggage key | Teams source | Auto-populated by `FromCoreActivity` / `FromTeamsContext`? |
|---|---|---|
| `microsoft.channel.name` (Required) | `Activity.ChannelId` (the whole string) | **Yes** |
| `microsoft.channel.link` (Optional in all four cert scopes) | No equivalent on the Teams activity | **No** — left unset by the extractor |

`microsoft.channel.link` is **Optional** in every cert-scope manifest, so leaving it unset does not block certification. The `ChannelLink(string?)` fluent setter remains on both `CoreBaggageBuilder` and `TeamsBaggageBuilder` for callers who do have a meaningful sub-channel value (for example, derived in HTTP middleware before the bot pipeline runs, or supplied from configuration).

We deliberately avoid synthesizing `ChannelLink` from `TeamsChannelData.Channel.Id` (the Teams team/channel id) or from `ServiceUrl`: the upstream semantics of `microsoft.channel.link` is "the sub-channel within the channel" (`M365CopilotSubChannel`-style routing), which is a different concept from a Teams channel id. Misclassifying these would mis-categorize spans in Agent365 dashboards.

### Schema change: `TenantId` on Core's `ConversationAccount`

To let Core-only bots populate `microsoft.tenant.id` (a Required cert key) without depending on Apps's `TeamsConversationAccount`, we promote `TenantId` to a typed property on Core's `ConversationAccount`:

```csharp
// Microsoft.Teams.Core/Schema/ConversationAccount.cs
[JsonPropertyName("tenantId")]
public string? TenantId { get; set; }
```

`TeamsConversationAccount` (Apps) loses its custom `Properties["tenantId"]` shim — the inherited typed property replaces it. `tenantId` is a cross-channel concept (the M365 tenant the conversation belongs to), not a Teams-specific extension; promoting it is consistent with how Agent Builder's schema treats it.

**Wire-format note:** classic Bot Framework Teams traffic carries tenant id in `channelData.tenant.id`, **not** at `from.tenantId` / `recipient.tenantId`. The schema change does not auto-populate `Recipient.TenantId` from such activities — both `CoreBaggageBuilder.FromCoreActivity` and `TeamsBaggageBuilder.FromTeamsContext` therefore fall back to `channelData.tenant.id` when the typed field is null. In Apps, the fallback uses the typed `TeamsActivity.ChannelData?.Tenant?.Id`. In Core, it parses `Activity.Properties["channelData"]` as JSON and extracts `tenant.id`.

### Bridging strategy

**Two layer-specific baggage builders, one per assembly.** Each layer ships its own independent baggage builder class shaped by the activity model that layer owns: `CoreBaggageBuilder` in Core, `TeamsBaggageBuilder` in Apps. Distinct names, no inheritance, no cross-references — each is self-contained. This honors the layering rule: neither builder downcasts to types it doesn't own.

The two field-set partitions:

| Field set | Source | Lives on which layer's builder |
|---|---|---|
| `microsoft.tenant.id`, `gen_ai.conversation.id`, `microsoft.conversation.item.link`, `microsoft.channel.name`, `gen_ai.agent.id`, `gen_ai.agent.name`, `microsoft.agent.user.id` (from `AgenticUserId`), `microsoft.a365.agent.blueprint.id`, `user.name`, `service.name`, `server.address`, `server.port` | `CoreActivity` + `ConversationAccount` (post-schema-change) | Core |
| Everything in Core **plus** `user.id` (from `AadObjectId`), `user.email`, `gen_ai.agent.description` (from `UserRole`), `microsoft.agent.user.email` | `TeamsActivity` + `TeamsConversationAccount` | Apps |

Apps's class duplicates Core's setter bodies (each is `Set(key, value); return this;`). The duplication is acceptable because the surface is small, the wire keys are part of an external (Agent365) contract that we cannot accidentally drift on without breaking exports, and it preserves clean independence between the layers.

#### Proposed surface

```csharp
// Microsoft.Teams.Core/Diagnostics/CoreBaggageBuilder.cs  (public)
namespace Microsoft.Teams.Core.Diagnostics;

public sealed class CoreBaggageBuilder
{
    // Keys reachable from CoreActivity / ConversationAccount.
    public CoreBaggageBuilder TenantId(string? v);
    public CoreBaggageBuilder ConversationId(string? v);
    public CoreBaggageBuilder ConversationItemLink(string? v);     // from ServiceUrl
    public CoreBaggageBuilder ChannelName(string? v);              // from ChannelId (string)
    public CoreBaggageBuilder ChannelLink(string? v);              // caller-supplied — no auto source
    public CoreBaggageBuilder AgentId(string? v);                  // Recipient.AgenticAppId ?? Recipient.Id
    public CoreBaggageBuilder AgentName(string? v);                // Recipient.Name
    public CoreBaggageBuilder AgenticUserId(string? v);            // Recipient.AgenticUserId
    public CoreBaggageBuilder AgentBlueprintId(string? v);         // Recipient.AgenticAppBlueprintId
    public CoreBaggageBuilder UserName(string? v);                 // From.Name
    public CoreBaggageBuilder OperationSource(string source);      // service.name — caller-supplied
    public CoreBaggageBuilder InvokeAgentServer(string? address, int? port = null);
    public CoreBaggageBuilder Set(string key, string? value);      // escape hatch

    /// <summary>Populates every setter above whose source field is non-null on <paramref name="activity"/>.
    /// Falls back to parsing <c>Properties["channelData"]</c> JSON for tenant id when
    /// <c>Recipient.TenantId</c> is empty.</summary>
    public CoreBaggageBuilder FromCoreActivity(CoreActivity activity);

    public IDisposable Build();  // applies pairs to OpenTelemetry.Baggage.Current; returns restore-scope
}
```

```csharp
// Microsoft.Teams.Apps/Diagnostics/TeamsBaggageBuilder.cs  (public — separate class)
namespace Microsoft.Teams.Apps.Diagnostics;

public sealed class TeamsBaggageBuilder
{
    // Same setters as Core's class …
    public TeamsBaggageBuilder TenantId(string? v);
    public TeamsBaggageBuilder ConversationId(string? v);
    public TeamsBaggageBuilder ConversationItemLink(string? v);
    public TeamsBaggageBuilder ChannelName(string? v);
    public TeamsBaggageBuilder ChannelLink(string? v);
    public TeamsBaggageBuilder AgentId(string? v);
    public TeamsBaggageBuilder AgentName(string? v);
    public TeamsBaggageBuilder AgenticUserId(string? v);
    public TeamsBaggageBuilder AgentBlueprintId(string? v);
    public TeamsBaggageBuilder UserName(string? v);
    public TeamsBaggageBuilder OperationSource(string source);
    public TeamsBaggageBuilder InvokeAgentServer(string? address, int? port = null);
    public TeamsBaggageBuilder Set(string key, string? value);

    // … plus setters whose source field only exists on TeamsConversationAccount:
    public TeamsBaggageBuilder UserId(string? v);                   // From.AadObjectId
    public TeamsBaggageBuilder UserEmail(string? v);                // From.Email
    public TeamsBaggageBuilder AgentDescription(string? v);         // Recipient.UserRole
    public TeamsBaggageBuilder AgenticUserEmail(string? v);         // Recipient.Email

    /// <summary>Populates every setter above whose source field is non-null on <c>ctx.Activity</c>,
    /// reading TeamsConversationAccount-only fields without any downcast (the activity already
    /// types From / Recipient as TeamsConversationAccount). Tenant fallback uses the typed
    /// <c>TeamsActivity.ChannelData?.Tenant?.Id</c>.</summary>
    public TeamsBaggageBuilder FromTeamsContext<TActivity>(Context<TActivity> ctx) where TActivity : TeamsActivity;

    public IDisposable Build();
}
```

The distinct names (`CoreBaggageBuilder` / `TeamsBaggageBuilder`) eliminate ambiguity when both namespaces are imported: a Core-only bot writes `new CoreBaggageBuilder()…`, a Teams-router bot writes `new TeamsBaggageBuilder()…`.

The Agent365 wire keys are duplicated as `internal const` strings in each assembly (`Microsoft.Teams.Core.Diagnostics.AgentObservabilityKeys` and the Apps equivalent) — same string values on both sides, kept in sync against the upstream Agent365 spec. They are not part of the public API of either assembly.

#### Consumer site

```csharp
// Apps-layer (Teams router) bot — this is the recommended path for Agent365.
using Microsoft.Teams.Apps.Diagnostics;

botApp.OnMessage(async (ctx, ct) =>
{
    using IDisposable scope = new TeamsBaggageBuilder()
        .FromTeamsContext(ctx)
        .OperationSource("teams-bot")     // required-for-cert; not derivable from the activity
        .Build();

    // … handler body — every span emitted from here carries Agent365 baggage.
});
```

```csharp
// Core-only bot (BotApplication.OnActivity, no Apps router).
// All Required cert keys are reachable from Core after the TenantId schema change,
// EXCEPT user.id, user.email, microsoft.agent.user.email (those need Apps's
// TeamsConversationAccount). A Core-only bot can either set them manually:
using Microsoft.Teams.Core.Diagnostics;

botApp.OnActivity = async (activity, ct) =>
{
    using IDisposable scope = new CoreBaggageBuilder()
        .FromCoreActivity(activity)
        .Set(/* user.id  */ "user.id",  myAadObjectIdFromAuth)
        .Set(/* user.email */ "user.email", myUserEmailFromAuth)
        .Set(/* agent.email */ "microsoft.agent.user.email", myAgentEmailFromConfig)
        .OperationSource("teams-bot")
        .Build();
    // …
};
```

The Apps router builder is the intended path for full cert; Core's builder covers most of the surface and provides the `Set(key, value)` escape hatch for the remainder.

#### Dependency impact

`Microsoft.Teams.Core` picks up one new `PackageReference`: **`OpenTelemetry.Api`** (the lightweight API contract package, no SDK, no exporters — already a transitive dep of every `Microsoft.OpenTelemetry` consumer; conventional dep for libraries that publish OTel signals: Azure SDK, gRPC, MongoDB driver). `Build()` writes to `OpenTelemetry.Baggage.Current`, which is the canonical OTel baggage that the distro propagates onto every span emitted in the scope.

`Microsoft.Teams.Apps` takes no new direct deps — `OpenTelemetry.Api` flows through transitively via Core's project reference.

## Why no DI plumbing

`ActivitySource` and `Meter` are process-global by design — `ActivityListener` and `MeterListener` subscribe by source/meter name, not by instance. The SDK therefore owns the singletons as `static readonly` fields and does not register them in DI. Consuming code never receives an `ActivitySource` parameter; it just registers the source name once at startup.

This keeps the instrumentation completely transparent: a bot that ignores the source name pays no overhead beyond the BCL's already-cheap "no listener attached" fast path. A bot that subscribes gets full traces, metrics, and trace-correlated logs.
