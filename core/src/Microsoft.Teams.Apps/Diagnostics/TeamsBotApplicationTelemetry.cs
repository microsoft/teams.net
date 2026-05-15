// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Apps.Diagnostics;

/// <summary>
/// Names of the <see cref="System.Diagnostics.ActivitySource"/> and <see cref="System.Diagnostics.Metrics.Meter"/>
/// emitted by <c>Microsoft.Teams.Apps</c>.
/// </summary>
/// <remarks>
/// Consumers register these names with their OpenTelemetry tracer and meter providers so that the
/// Teams-application-level spans (<c>handler</c>) flow to configured exporters. Lower-level layers
/// (<c>Microsoft.Teams.Core</c>) publish their own source/meter; register them all to capture the full
/// bot pipeline.
/// <code>
/// builder.Services.AddOpenTelemetry()
///     .WithTracing(t => t
///         .AddSource(CoreTelemetryNames.ActivitySourceName)
///         .AddSource(TeamsBotApplicationTelemetry.ActivitySourceName))
///     .WithMetrics(m => m
///         .AddMeter(CoreTelemetryNames.MeterName)
///         .AddMeter(TeamsBotApplicationTelemetry.MeterName));
/// </code>
/// </remarks>
public static class TeamsBotApplicationTelemetry
{
    /// <summary>
    /// Name of the <see cref="System.Diagnostics.ActivitySource"/> that emits Apps-level spans.
    /// </summary>
    public const string ActivitySourceName = "Microsoft.Teams.Apps";

    /// <summary>
    /// Name of the <see cref="System.Diagnostics.Metrics.Meter"/> that emits Apps-level metrics.
    /// </summary>
    public const string MeterName = "Microsoft.Teams.Apps";
}
