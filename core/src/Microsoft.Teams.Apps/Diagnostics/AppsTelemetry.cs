// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;

namespace Microsoft.Teams.Apps.Diagnostics;

/// <summary>
/// Singletons for the Apps-level <see cref="ActivitySource"/>, <see cref="Meter"/>, and instruments.
/// Internal to <c>Microsoft.Teams.Apps</c>.
/// </summary>
internal static class AppsTelemetry
{
    private static readonly string s_version =
        typeof(AppsTelemetry).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(AppsTelemetry).Assembly.GetName().Version?.ToString()
        ?? "0.0.0";

    public static readonly ActivitySource Source =
        new(TeamsBotApplicationTelemetry.ActivitySourceName, s_version);

    public static readonly Meter Meter =
        new(TeamsBotApplicationTelemetry.MeterName, s_version);

    public static readonly Counter<long> HandlerDispatched =
        Meter.CreateCounter<long>(Metrics.HandlerDispatched, description: "Total handler invocations dispatched by the router.");

    public static readonly Histogram<double> HandlerDuration =
        Meter.CreateHistogram<double>(Metrics.HandlerDuration, unit: "ms", description: "Duration of individual handler invocations.");

    public static readonly Counter<long> HandlerFailures =
        Meter.CreateCounter<long>(Metrics.HandlerFailures, description: "Total handler invocations that threw an exception.");

    public static readonly Counter<long> HandlerUnmatched =
        Meter.CreateCounter<long>(Metrics.HandlerUnmatched, description: "Total activities that found no matching route.");

    public static class Spans
    {
        public const string Handler = "handler";
    }

    public static class Tags
    {
        public const string HandlerType = "handler.type";
        public const string HandlerDispatch = "handler.dispatch";
        public const string ActivityType = "activity.type";
        public const string InvokeName = "invoke.name";
    }

    public static class Metrics
    {
        public const string HandlerDispatched = "teams.handler.dispatched";
        public const string HandlerDuration = "teams.handler.duration";
        public const string HandlerFailures = "teams.handler.failures";
        public const string HandlerUnmatched = "teams.handler.unmatched";
    }
}
