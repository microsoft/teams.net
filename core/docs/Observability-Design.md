# Observability Design

## Overview

The SDK emits OpenTelemetry-compatible traces, metrics, and logs through the BCL `ActivitySource` and `Meter` APIs. The SDK provides the signal names and instrumentation; the consuming app wires exporters through the Microsoft OpenTelemetry distro.

`Microsoft.Teams.Core` owns the low-level turn pipeline and outbound client signals (`conversation_client`, `user_token_client`). `Microsoft.Teams.Apps` owns router, state, and OAuth signals. `Microsoft.Teams.Apps` also depends on `OpenTelemetry.Api` so it can write baggage for Agent365 scenarios.

## Signal names

| Layer | Source / Meter | Main signals |
|---|---|---|
| Core | `Microsoft.Teams.Core` | `turn`, `middleware`, `auth.outbound`, `conversation_client`, `user_token_client`; `teams.activities.received`, `teams.turn.duration`, `teams.handler.errors`, `teams.middleware.duration`, `teams.outbound.calls`, `teams.outbound.errors`, `teams.outbound.duration` |
| Apps | `Microsoft.Teams.Apps` | `handler`, `state`, `oauth`, `team_client`, `meeting_client`; `teams.handler.dispatched`, `teams.handler.duration`, `teams.handler.failures`, `teams.handler.unmatched`, `teams.oauth.operations`, `teams.oauth.operation.duration`, `teams.oauth.errors` |

## Spans

The HTTP-server span from ASP.NET Core is the parent of `turn`. Outbound HTTP-client spans are children of `auth.outbound` and `conversation_client`. The Apps `handler` span nests inside `turn`.

| Span | Where it starts | Notes |
|---|---|---|
| `turn` | `BotApplication.ProcessAsync` | Sets activity/conversation tags |
| `middleware` | `TurnMiddleware.RunPipelineAsync` | One span per middleware |
| `conversation_client` | `ConversationClient` | Covers all conversation operations (send/update/delete, members, history, upload, reactions, create conversation) |
| `user_token_client` | `UserTokenClient` | Covers token service operations |
| `handler` | `Router.DispatchAsync` / `DispatchWithReturnAsync` | Covers matched route execution; `handler.type` is the route name |
| `state` | `TurnStateLoader` | Covers load/save/delete (disambiguated by `operation` tag) |
| `auth.outbound` | `BotAuthenticationHandler` | Covers token acquisition |
| `oauth` | `OAuthFlow` | Covers sign-in, sign-out, token exchange, verify-state, signin-failure, and connection status (disambiguated by `oauth.operation` tag) |
| `team_client` | `TeamClient` | Covers Teams API calls (`/v3/teams/...`) |
| `meeting_client` | `MeetingClient` | Covers Meetings API calls (`/v1/meetings/...`) |

## Tags

Tags are custom properties attached to spans and metrics (for example, `customDimensions` in Application Insights).

| Tag | Used on | Purpose |
|---|---|---|
| `client` | Core outbound spans/metrics; team/meeting client spans | Distinguishes caller (`conversation`, `user_token`, `team`, `meeting`) |
| `operation` | Core outbound spans/metrics; state span | Distinguishes API or state operation within a client/span |
| `handler.type` | `handler` span and handler metrics | Route name selected by router |
| `activity.type` | Turn and unmatched-handler telemetry | Activity category (message, invoke, etc.) |
| `invoke.name` | `teams.handler.unmatched` (invoke path) | Invoke verb/name when no route matched |
| `oauth.operation` | `oauth` span and OAuth metrics | OAuth flow step (`signin`, `get_token`, `verify_state`, etc.) |
| `oauth.result` | `oauth` span and OAuth operation metric | Outcome (`token_found`, `token_not_found`, `operation_succeeded`, etc.) |
| `service.url` | Outbound client spans | Target Bot Service/Token Service endpoint host |

### Span hierarchy per turn

```
HTTP server span (auto, ASP.NET Core)
└─ turn (Core)
   ├─ middleware [0..n] (Core)
   ├─ handler (Apps, when using TeamsBotApplication)
   │  ├─ state [0..n] (Apps, when state is enabled; operation in `operation` tag)
   │  └─ oauth [0..n] (Apps, when OAuth flow is used; operation in `oauth.operation` tag)
   │     ├─ user_token_client (Core)
   │     │  ├─ auth.outbound (Core)
   │     │  │  └─ HTTP client span (auto, token acquisition endpoint; when network token fetch occurs)
   │     │  └─ HTTP client span (auto, Bot Framework Token Service)
   │     └─ conversation_client (Core, for OAuth-card send path)
   │        ├─ auth.outbound (Core)
   │        │  └─ HTTP client span (auto, token acquisition endpoint; when network token fetch occurs)
   │        └─ HTTP client span (auto, Bot Service API)
   ├─ team_client [0..n] (Apps, Teams API client)
   │  ├─ auth.outbound (Core)
   │  │  └─ HTTP client span (auto, token acquisition endpoint; when network token fetch occurs)
   │  └─ HTTP client span (auto, Bot Service API /v3/teams)
   ├─ meeting_client [0..n] (Apps, Meetings API client)
   │  ├─ auth.outbound (Core)
   │  │  └─ HTTP client span (auto, token acquisition endpoint; when network token fetch occurs)
   │  └─ HTTP client span (auto, Bot Service API /v1/meetings)
   ├─ conversation_client [0..n] (Core, send/update/delete/members/history/upload/reactions/create)
   │  ├─ auth.outbound (Core)
   │  │  └─ HTTP client span (auto, token acquisition endpoint; when network token fetch occurs)
   │  └─ HTTP client span (auto, Bot Service API)
   └─ user_token_client [0..n] (Core, token APIs)
      ├─ auth.outbound (Core)
      │  └─ HTTP client span (auto, token acquisition endpoint; when network token fetch occurs)
      └─ HTTP client span (auto, Bot Framework Token Service)
```

## Metrics

Core records turn and outbound client metrics. Apps records router and OAuth metrics. Error counters are only incremented for unexpected failures; protocol fallbacks like "no token yet" are recorded as normal OAuth outcomes, not errors.

`team_client` and `meeting_client` spans are Apps-level, but their outbound counters/histograms are recorded in Core `teams.outbound.*` with `client=team|meeting`, so customers can query one outbound metric family across Conversation, UserToken, Team, and Meeting clients.

## Logs

Logs are already trace-correlated through the OTel distro. `BotApplication` adds turn-scoped fields such as activity type, activity id, service URL, and MSCV to the log scope, so no extra logging plumbing is needed.

## Agent365 baggage

For Agent365, Apps ships `TeamsBaggageBuilder`. It reads Teams activity data directly and writes the required baggage into `OpenTelemetry.Baggage.Current`.

This is intentionally **not** built on `ITurnContext`: Teams handlers receive `Context<TeamsActivity>`, not `ITurnContext`, and the Teams activity model does not have Agent Builder's `ChannelId.Channel/SubChannel` shape. `TeamsBaggageBuilder` handles the Teams equivalents and leaves truly optional values unset.

## Consumer setup

```csharp
builder.Services.AddOpenTelemetry()
    .UseMicrosoftOpenTelemetry()
    .WithTracing(t => t
        .AddSource(CoreTelemetryNames.ActivitySourceName)
        .AddSource(TeamsBotApplicationTelemetry.ActivitySourceName))
    .WithMetrics(m => m
        .AddMeter(CoreTelemetryNames.MeterName)
        .AddMeter(TeamsBotApplicationTelemetry.MeterName));
```

The sample app is `core/samples/ObservabilityBot/`.
