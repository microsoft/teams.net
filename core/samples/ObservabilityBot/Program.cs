// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.OpenTelemetry;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Diagnostics;
using Microsoft.Teams.Core.Hosting;
using Microsoft.Teams.Core.Schema;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

// Each Teams SDK layer publishes its own ActivitySource / Meter; register all you use.
// Microsoft.Teams.Core              -> turn / middleware / auth.outbound / conversation_client
// Microsoft.Teams.Apps              -> handler  (only if you use the Apps router; not in this sample)
string[] activitySources = [TeamsCoreTelemetry.ActivitySourceName];
string[] meterNames      = [TeamsCoreTelemetry.MeterName];

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddBotApplication();

// Wire the Microsoft OpenTelemetry distro and subscribe to the Teams SDK's
// ActivitySource and Meter. Exporters are auto-detected from environment:
//   APPLICATIONINSIGHTS_CONNECTION_STRING  -> Azure Monitor
//   OTEL_EXPORTER_OTLP_ENDPOINT            -> OTLP collector (Aspire / Grafana LGTM / Jaeger)
// Console export is enabled below for local debugging.
builder.Services.AddOpenTelemetry()
    .UseMicrosoftOpenTelemetry(o => o.Exporters = ExportTarget.Otlp)
    .WithTracing(t => t.AddSource(activitySources))
    .WithMetrics(m => m.AddMeter(meterNames));

builder.Logging.AddOpenTelemetry(o => o.IncludeFormattedMessage = true);

WebApplication app = builder.Build();

app.MapGet("/", () => "ObservabilityBot is running. Telemetry source: " + TeamsCoreTelemetry.ActivitySourceName);

BotApplication botApp = app.UseBotApplication();

botApp.OnActivity = async (activity, cancellationToken) =>
{
    ArgumentNullException.ThrowIfNull(activity.Conversation);

    CoreActivity reply = CoreActivity.CreateBuilder()
        .WithType(ActivityType.Message)
        .WithChannelId(activity.ChannelId)
        .WithServiceUrl(activity.ServiceUrl)
        .WithConversation(activity.Conversation)
        .WithFrom(activity.Recipient)
        .WithProperty("text", $"ObservabilityBot received `{activity.Type}` (SDK {BotApplication.Version}).")
        .Build();

    await botApp.SendActivityAsync(reply, cancellationToken: cancellationToken);
};

app.Run();
