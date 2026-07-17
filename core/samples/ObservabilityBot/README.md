# ObservabilityBot

Minimal Teams bot wired to the [`Microsoft.OpenTelemetry`](https://github.com/microsoft/opentelemetry-distro-dotnet) distro. Demonstrates how a consuming app subscribes to the Teams SDK's `ActivitySource` and `Meter` so that turn / middleware / handler / auth.outbound / conversation_client spans and the `teams.*` metrics flow to configured exporters alongside auto-instrumented HTTP server / client / Azure SDK spans.

## Prerequisites

- Bot registered and installed in Teams.
- OpenTelemetry export target available (for local demo, Grafana LGTM).
- Azure OpenAI configuration (required by the sample's AI path).

## What it shows

```csharp
builder.Services.AddOpenTelemetry()
    .UseMicrosoftOpenTelemetry(o => o.Exporters = ExportTarget.Console | ExportTarget.Otlp)
    .WithTracing(t => t
        .AddSource(CoreTelemetryNames.ActivitySourceName)
        .AddSource(TeamsBotApplicationTelemetry.ActivitySourceName))
    .WithMetrics(m => m
        .AddMeter(CoreTelemetryNames.MeterName)
        .AddMeter(TeamsBotApplicationTelemetry.MeterName));

builder.Logging.AddOpenTelemetry(o => o.IncludeFormattedMessage = true);
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

# Required for the AI chat client (Azure OpenAI):
export AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com
export AZURE_OPENAI_KEY=your-key
export AZURE_OPENAI_DEPLOYMENT=your-deployment-name

dotnet run --project core/samples/ObservabilityBot
```

Open http://localhost:3000 (`admin` / `admin`) and explore Tempo, Mimir, and Loki.

## Send a test activity

To exercise the pipeline you need to POST a Bot Framework activity payload (with a valid bearer token) to the bot's `/api/messages` endpoint. Reasonable options:

- Use the `core/test/ABSTokenServiceClient` helper to mint a token, then `curl` a JSON activity.
- Drive the bot from one of the harnesses under `core/test/IntegrationTests`.
- Deploy the bot to a Teams tenant and chat with it.

## Export targets

- Set `APPLICATIONINSIGHTS_CONNECTION_STRING` to additionally export to Azure Monitor / Application Insights.
- Remove `ExportTarget.Console` for production.
- See the [Microsoft OpenTelemetry distro README](https://github.com/microsoft/opentelemetry-distro-dotnet#readme) for the full set of `ExportTarget` values, sampling, and Azure Monitor options.

## What you should see

Per turn, the trace has the shape:

```
HTTP server span                       (auto, OTel ASP.NET Core)
└─ turn                                (Microsoft.Teams.Core)
   ├─ middleware [n times]             (Microsoft.Teams.Core)
   ├─ handler                          (Microsoft.Teams.Apps)
   └─ conversation_client              (Microsoft.Teams.Core)
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
