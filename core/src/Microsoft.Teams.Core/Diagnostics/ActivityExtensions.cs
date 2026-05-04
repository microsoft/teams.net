// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;

namespace Microsoft.Teams.Core.Diagnostics;

/// <summary>
/// Helpers for setting standardized tags and recording exceptions on <see cref="Activity"/> instances
/// emitted by the Teams SDK's bot pipeline.
/// </summary>
internal static class ActivityExtensions
{
    /// <summary>
    /// Records an exception on the span: sets status to <see cref="ActivityStatusCode.Error"/> and
    /// adds an <c>exception</c> event with type/message/stacktrace tags. Mirrors the shape that
    /// <see cref="Activity.AddException"/> uses on .NET 9+ but works on net8.0 as well.
    /// </summary>
    public static void RecordException(this Activity? activity, Exception exception)
    {
        if (activity is null || exception is null)
        {
            return;
        }

        ActivityTagsCollection tags = new()
        {
            { "exception.type", exception.GetType().FullName },
            { "exception.message", exception.Message },
            { "exception.stacktrace", exception.ToString() },
        };
        activity.AddEvent(new ActivityEvent("exception", tags: tags));
        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
    }
}
