# ObservabilityBot

Minimal Teams bot wired to the [`Microsoft.OpenTelemetry`](https://github.com/microsoft/opentelemetry-distro-dotnet) distro. Demonstrates how a consuming app subscribes to the Teams SDK's `ActivitySource` and `Meter` so that turn / middleware / handler / auth.outbound / conversation_client spans and the `teams.*` metrics flow to configured exporters alongside auto-instrumented HTTP server / client / Azure SDK spans.

## Prerequisites

- Bot registered and installed in Teams.
- OpenTelemetry export target available (for local demo, Grafana LGTM).
- Azure OpenAI configured:
  - `AzureOpenAI__Endpoint`
  - `AzureOpenAI__ApiKey`
  - `AzureOpenAI__Deployment` 
- OAuth connection named `sso` configured on the bot resource.
- [optional for multiple instances] Redis available and configured through `ConnectionStrings__Redis`.

## What it shows

```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r
        .AddService(serviceName: "ObservabilityBot", serviceVersion: "0.0.1")
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = builder.Environment.EnvironmentName,
            ["service.namespace"] = "Microsoft.Teams"
        }))
    .UseMicrosoftOpenTelemetry(o =>
    {
        o.Exporters = ExportTarget.Otlp | ExportTarget.AzureMonitor;
        o.Instrumentation.EnableHttpClientInstrumentation = true;
        o.Instrumentation.EnableAspNetCoreInstrumentation = true;
    })
    .WithTracing(t => t.AddSource(activitySources))
    .WithMetrics(m => m.AddMeter(meterNames));
```

The two `.AddSource` / `.AddMeter` calls are the only Teams-specific OTel wiring. Everything else is standard distro setup.

## Run locally with Grafana LGTM (traces + metrics + logs)

[`grafana/otel-lgtm`](https://github.com/grafana/docker-otel-lgtm) is a single container that bundles Tempo (traces), Mimir (metrics), Loki (logs), and Grafana, and accepts OTLP on ports 4317 (gRPC) and 4318 (HTTP).

```bash
docker run --rm -d --name lgtm \
  -p 3000:3000 -p 4317:4317 -p 4318:4318 \
  grafana/otel-lgtm

export OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
export OTEL_SERVICE_NAME=teams-observability-bot
export OTEL_RESOURCE_ATTRIBUTES="deployment.environment=local,service.version=dev"

dotnet run --project core/samples/ObservabilityBot
```

Open http://localhost:3000 (`admin` / `admin`) and explore Tempo, Mimir, and Loki.

## Send a test activity

- Deploy the bot to a Teams tenant and chat with it.

Then use these commands in chat:
- `help`
- `login` / `logout` / `status` (OAuth flow telemetry)
- `team` (TeamClient telemetry)

## Export targets

- Set `ConnectionStrings__AppInsights` to additionally export to Azure Monitor / Application Insights.
- See the [Microsoft OpenTelemetry distro README](https://github.com/microsoft/opentelemetry-distro-dotnet#readme) for the full set of `ExportTarget` values, sampling, and Azure Monitor options.

## What you should see

Per turn, the trace has the shape:

```
HTTP server span                       (auto, OTel ASP.NET Core)
└─ turn                                (Microsoft.Teams.Core)
   ├─ middleware [n times]             (Microsoft.Teams.Core)
   ├─ handler                          (Microsoft.Teams.Apps)
   ├─ oauth                            (Microsoft.Teams.Apps, when login/status/logout runs)
   │  └─ user_token_client             (Microsoft.Teams.Core)
   ├─ team_client                      (Microsoft.Teams.Apps, when team runs)
   └─ conversation_client              (Microsoft.Teams.Core, AI responses / sends)
      ├─ auth.outbound                 (Microsoft.Teams.Core)
      │  └─ HTTP client span           (auto — token endpoint)
      └─ HTTP client span              (auto — Bot Service API)
```

Metrics (Prometheus / Mimir names): `teams_activities_received_total`, `teams_turn_duration_milliseconds_bucket/sum/count`, `teams_handler_errors_total`, `teams_middleware_duration_milliseconds_*`, `teams_outbound_calls_total`, `teams_outbound_errors_total`.

Logs: every `ILogger` record produced inside a turn carries the active `TraceId` / `SpanId` so Loki queries can pivot from a slow trace to its log lines.

## Running the Sample

~~~bash
dotnet run --project samples/ObservabilityBot/ObservabilityBot.csproj
~~~
