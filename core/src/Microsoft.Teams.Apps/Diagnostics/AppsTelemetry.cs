// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Microsoft.Teams.Apps.Diagnostics;

/// <summary>
/// Singletons for the Apps-level <see cref="ActivitySource"/>, <see cref="Meter"/>, and instruments.
/// Internal to <c>Microsoft.Teams.Apps</c>.
/// </summary>
internal static class AppsTelemetry
{
    private const string s_version = ThisAssembly.NuGetPackageVersion;

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

    // ── State instruments ────────────────────────────────────────────────

    public static readonly Histogram<double> StateLoadDuration =
        Meter.CreateHistogram<double>(Metrics.StateLoadDuration, unit: "ms", description: "Duration of state load from cache.");

    public static readonly Histogram<double> StateSaveDuration =
        Meter.CreateHistogram<double>(Metrics.StateSaveDuration, unit: "ms", description: "Duration of state save to cache.");

    public static readonly Counter<long> StateCacheErrors =
        Meter.CreateCounter<long>(Metrics.StateCacheErrors, description: "Total cache operation failures for turn state.");

    public static readonly Histogram<long> StateBytesRead =
        Meter.CreateHistogram<long>(Metrics.StateBytesRead, unit: "By", description: "Bytes read from cache per state load.");

    public static readonly Histogram<long> StateBytesWritten =
        Meter.CreateHistogram<long>(Metrics.StateBytesWritten, unit: "By", description: "Bytes written to cache per state save.");

    public static class Spans
    {
        public const string Handler = "handler";
        public const string StateLoad = "state.load";
        public const string StateSave = "state.save";
        public const string StateDelete = "state.delete";
    }

    public static class Tags
    {
        public const string HandlerType = "handler.type";
        public const string HandlerDispatch = "handler.dispatch";
        public const string ActivityType = "activity.type";
        public const string InvokeName = "invoke.name";

        // State tags
        public const string StateConversationHit = "state.conversation.hit";
        public const string StateUserHit = "state.user.hit";
        public const string StateConversationDirty = "state.conversation.dirty";
        public const string StateUserDirty = "state.user.dirty";
        public const string StateBytesRead = "state.bytes.read";
        public const string StateBytesWritten = "state.bytes.written";
        public const string Operation = "operation";
    }

    public static class Metrics
    {
        public const string HandlerDispatched = "teams.handler.dispatched";
        public const string HandlerDuration = "teams.handler.duration";
        public const string HandlerFailures = "teams.handler.failures";
        public const string HandlerUnmatched = "teams.handler.unmatched";

        // State metrics
        public const string StateLoadDuration = "teams.state.load.duration";
        public const string StateSaveDuration = "teams.state.save.duration";
        public const string StateCacheErrors = "teams.state.cache.errors";
        public const string StateBytesRead = "teams.state.bytes.read";
        public const string StateBytesWritten = "teams.state.bytes.written";
    }
}
