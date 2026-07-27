// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Microsoft.Teams.Core.Diagnostics;

/// <summary>
/// Singletons for the SDK's <see cref="ActivitySource"/>, <see cref="Meter"/>, and instruments.
/// Internal to <c>Microsoft.Teams.Core</c>.
/// </summary>
internal static class Telemetry
{
    private const string s_version = ThisAssembly.NuGetPackageVersion;

    public static readonly ActivitySource Source =
        new(CoreTelemetryNames.ActivitySourceName, s_version);

    public static readonly Meter Meter =
        new(CoreTelemetryNames.MeterName, s_version);

    public static readonly Counter<long> ActivitiesReceived =
        Meter.CreateCounter<long>("teams.activities.received", description: "Total activities received by the bot.");

    public static readonly Histogram<double> TurnDuration =
        Meter.CreateHistogram<double>("teams.turn.duration", unit: "ms", description: "Duration of full turn processing.");

    public static readonly Counter<long> HandlerErrors =
        Meter.CreateCounter<long>("teams.handler.errors", description: "Total exceptions thrown during turn processing.");

    public static readonly Histogram<double> MiddlewareDuration =
        Meter.CreateHistogram<double>("teams.middleware.duration", unit: "ms", description: "Duration of individual middleware execution.");

    public static readonly Counter<long> OutboundCalls =
        Meter.CreateCounter<long>("teams.outbound.calls", description: "Total outbound Core HTTP client calls.");

    public static readonly Counter<long> OutboundErrors =
        Meter.CreateCounter<long>("teams.outbound.errors", description: "Total outbound Core HTTP client call errors.");

    public static readonly Histogram<double> OutboundDuration =
        Meter.CreateHistogram<double>("teams.outbound.duration", unit: "ms", description: "Duration of Core HTTP client calls.");

    /// <summary>
    /// Span names for telemetry emitted by the SDK. These are used to identify spans in traces.
    /// </summary>
    public static class Spans
    {
        public const string Turn = "turn";
        public const string Middleware = "middleware";
        public const string AuthOutbound = "auth.outbound";
        public const string Client = "client";
    }

    /// <summary>
    /// Custom tag names for telemetry emitted by the SDK. These are used to add additional context to spans and metrics.
    /// </summary>
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
        public const string Client = "client.name";
        public const string ClientOperation = "client.operation";
    }

    /// <summary>
    /// Values for the <c>client.operation</c> tag, which represents the operation being performed by the SDK's <c>conversation</c> and <c>user_token</c> clients.
    /// </summary>
    public static class ClientOperations
    {
        public const string SendActivity = "sendActivity";
        public const string UpdateActivity = "updateActivity";
        public const string DeleteActivity = "deleteActivity";
        public const string GetConversationMembers = "getConversationMembers";
        public const string GetConversationMember = "getConversationMember";
        public const string GetConversations = "getConversations";
        public const string GetActivityMembers = "getActivityMembers";
        public const string CreateConversation = "createConversation";
        public const string GetConversationPagedMembers = "getConversationPagedMembers";
        public const string DeleteConversationMember = "deleteConversationMember";
        public const string SendConversationHistory = "sendConversationHistory";
        public const string UploadAttachment = "uploadAttachment";
        public const string AddReaction = "addReaction";
        public const string DeleteReaction = "deleteReaction";
        public const string GetTokenStatus = "getTokenStatus";
        public const string GetToken = "getToken";
        public const string GetSignInResource = "getSignInResource";
        public const string GetSignInUrl = "getSignInUrl";
        public const string ExchangeToken = "exchangeToken";
        public const string SignOutUser = "signOutUser";
        public const string GetAadTokens = "getAadTokens";
    }

    /// <summary>
    /// Values for the <c>client.name</c> tag, which represents the SDK's <c>conversation</c> and <c>user_token</c> clients.
    /// </summary>
    public static class Clients
    {
        public const string Conversation = "conversation";
        public const string UserToken = "user_token";
    }
}
