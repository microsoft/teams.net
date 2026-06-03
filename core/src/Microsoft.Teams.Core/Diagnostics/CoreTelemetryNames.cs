// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Core.Diagnostics;

/// <summary>
/// Names of the <see cref="System.Diagnostics.ActivitySource"/> and <see cref="System.Diagnostics.Metrics.Meter"/>
/// emitted by <c>Microsoft.Teams.Core</c>.
/// </summary>
/// <remarks>
/// Consumers register these names with their OpenTelemetry tracer and meter providers so that the bot
/// pipeline spans (<c>turn</c>, <c>middleware</c>, <c>auth.outbound</c>, <c>conversation_client</c>) and metrics
/// (<c>teams.activities.received</c>, <c>teams.turn.duration</c>, <c>teams.handler.errors</c>,
/// <c>teams.middleware.duration</c>, <c>teams.outbound.calls</c>, <c>teams.outbound.errors</c>) flow to
/// configured exporters. Higher-level layers publish their own sources (for example,
/// <c>Microsoft.Teams.Apps.Diagnostics.TeamsBotApplicationTelemetry</c>); register them all to capture
/// the full bot pipeline.
/// <code>
/// builder.Services.AddOpenTelemetry()
///     .WithTracing(t => t.AddSource(CoreTelemetryNames.ActivitySourceName))
///     .WithMetrics(m => m.AddMeter(CoreTelemetryNames.MeterName));
/// </code>
/// </remarks>
public static class CoreTelemetryNames
{
    /// <summary>
    /// Name of the <see cref="System.Diagnostics.ActivitySource"/> that emits Core pipeline spans.
    /// </summary>
    public const string ActivitySourceName = "Microsoft.Teams.Core";

    /// <summary>
    /// Name of the <see cref="System.Diagnostics.Metrics.Meter"/> that emits Core pipeline metrics.
    /// </summary>
    public const string MeterName = "Microsoft.Teams.Core";
}
