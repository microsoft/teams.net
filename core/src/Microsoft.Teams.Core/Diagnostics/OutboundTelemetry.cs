// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;

namespace Microsoft.Teams.Core.Diagnostics;

/// <summary>
/// Helpers for recording outbound client telemetry into the Core metric set.
/// </summary>
public static class OutboundTelemetry
{
    /// <summary>
    /// Records a successful outbound call.
    /// </summary>
    public static void RecordCall(string client, string operation)
        => Telemetry.OutboundCalls.Add(
            1,
            new KeyValuePair<string, object?>(Telemetry.Tags.Client, client),
            new KeyValuePair<string, object?>(Telemetry.Tags.Operation, operation));

    /// <summary>
    /// Records a failed outbound call and the exception on the active span.
    /// </summary>
    public static void RecordError(Activity? span, Exception ex, string client, string operation)
    {
        span.RecordException(ex);
        Telemetry.OutboundErrors.Add(
            1,
            new KeyValuePair<string, object?>(Telemetry.Tags.Client, client),
            new KeyValuePair<string, object?>(Telemetry.Tags.Operation, operation));
    }

    /// <summary>
    /// Records outbound call duration in milliseconds.
    /// </summary>
    public static void RecordDuration(long startTimestamp, string client, string operation)
        => Telemetry.OutboundDuration.Record(
            Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds,
            new KeyValuePair<string, object?>(Telemetry.Tags.Client, client),
            new KeyValuePair<string, object?>(Telemetry.Tags.Operation, operation));
}
