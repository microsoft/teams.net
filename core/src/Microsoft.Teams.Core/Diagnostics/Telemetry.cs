// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;

namespace Microsoft.Teams.Core.Diagnostics;

/// <summary>
/// Singletons for the SDK's <see cref="ActivitySource"/>, <see cref="Meter"/>, and instruments.
/// Internal to <c>Microsoft.Teams.Core</c>; visible to <c>Microsoft.Teams.Apps</c>
/// and <c>Microsoft.Teams.Apps.BotBuilder</c> via <c>InternalsVisibleTo</c>.
/// </summary>
internal static class Telemetry
{
    private const string s_version = ThisAssembly.NuGetPackageVersion;
        
    public static readonly ActivitySource Source =
        new(TeamsCoreTelemetry.ActivitySourceName, s_version);

    public static readonly Meter Meter =
        new(TeamsCoreTelemetry.MeterName, s_version);

    public static readonly Counter<long> ActivitiesReceived =
        Meter.CreateCounter<long>("teams.activities.received", description: "Total activities received by the bot.");

    public static readonly Histogram<double> TurnDuration =
        Meter.CreateHistogram<double>("teams.turn.duration", unit: "ms", description: "Duration of full turn processing.");

    public static readonly Counter<long> HandlerErrors =
        Meter.CreateCounter<long>("teams.handler.errors", description: "Total exceptions thrown during turn processing.");

    public static readonly Histogram<double> MiddlewareDuration =
        Meter.CreateHistogram<double>("teams.middleware.duration", unit: "ms", description: "Duration of individual middleware execution.");

    public static readonly Counter<long> OutboundCalls =
        Meter.CreateCounter<long>("teams.outbound.calls", description: "Total outbound Bot Service API calls.");

    public static readonly Counter<long> OutboundErrors =
        Meter.CreateCounter<long>("teams.outbound.errors", description: "Total outbound Bot Service API call errors.");

    // Span name constants — kept here so callers don't drift on naming.
    public static class Spans
    {
        public const string Turn = "turn";
        public const string Middleware = "middleware";
        public const string AuthOutbound = "auth.outbound";
        public const string ConversationClient = "conversation_client";
    }

    public static class Tags
    {
        public const string ActivityType = "activity.type";
        public const string ActivityId = "activity.id";
        public const string ConversationId = "conversation.id";
        public const string ChannelId = "channel.id";
        public const string BotId = "bot.id";
        public const string ServiceUrl = "service.url";
        public const string MiddlewareName = "middleware.name";
        public const string MiddlewareIndex = "middleware.index";
        public const string AuthFlow = "auth.flow";
        public const string AuthScope = "auth.scope";
        public const string Operation = "operation";
    }

    public static class Operations
    {
        public const string SendActivity = "sendActivity";
        public const string UpdateActivity = "updateActivity";
        public const string DeleteActivity = "deleteActivity";
    }
}
