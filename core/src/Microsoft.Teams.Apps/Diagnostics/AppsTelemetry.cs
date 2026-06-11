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

    // ── OAuth instruments ────────────────────────────────────────────────

    public static readonly Counter<long> OAuthOperationCount =
        Meter.CreateCounter<long>(Metrics.OAuthOperations, description: "Total OAuth flow operations attempted. For verify_state and signin_failure invokes, each per-flow attempt is counted independently in multi-connection deployments.");

    public static readonly Histogram<double> OAuthOperationDuration =
        Meter.CreateHistogram<double>(Metrics.OAuthOperationDuration, unit: "ms", description: "Duration of OAuth flow operations.");

    public static readonly Counter<long> OAuthErrors =
        Meter.CreateCounter<long>(Metrics.OAuthErrors, description: "Total OAuth flow operations that failed with an unexpected exception. Expected protocol fallbacks (HTTP 404/400/412 from the Token Service) are not counted here; they are recorded as oauth.result=failure on teams.oauth.operations instead.");

    public static class Spans
    {
        public const string Handler = "handler";
        public const string StateLoad = "state.load";
        public const string StateSave = "state.save";
        public const string StateDelete = "state.delete";

        // OAuth spans
        public const string OAuthSignIn = "oauth.signin";
        public const string OAuthSignOut = "oauth.signout";
        public const string OAuthGetToken = "oauth.get_token";
        public const string OAuthTokenExchange = "oauth.token_exchange";
        public const string OAuthVerifyState = "oauth.verify_state";
        public const string OAuthSignInFailure = "oauth.signin_failure";
        public const string OAuthConnectionStatus = "oauth.connection_status";
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

        // OAuth tags
        public const string OAuthConnection = "oauth.connection";
        public const string OAuthOperation = "oauth.operation";
        public const string OAuthResult = "oauth.result";
        public const string OAuthErrorType = "oauth.error.type";
        public const string OAuthFailureCode = "oauth.failure.code";
        public const string OAuthCallbackInvoked = "oauth.callback.invoked";
        public const string InvokeResponseStatus = "invoke.response.status";
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

        // OAuth metrics
        public const string OAuthOperations = "teams.oauth.operations";
        public const string OAuthOperationDuration = "teams.oauth.operation.duration";
        public const string OAuthErrors = "teams.oauth.errors";
    }

    /// <summary>
    /// Values used for the <see cref="Tags.OAuthOperation"/> tag.
    /// Low cardinality: one of seven well-known operation names.
    /// </summary>
    public static class OAuthOperations
    {
        public const string SignIn = "signin";
        public const string SignOut = "signout";
        public const string GetToken = "get_token";
        public const string TokenExchange = "token_exchange";
        public const string VerifyState = "verify_state";
        public const string SignInFailure = "signin_failure";
        public const string ConnectionStatus = "connection_status";
    }

    /// <summary>
    /// Values used for the <see cref="Tags.OAuthResult"/> tag.
    /// </summary>
    public static class OAuthResults
    {
        /// <summary>SignIn returned a cached token without sending an OAuthCard.</summary>
        public const string Cached = "cached";
        /// <summary>SignIn sent an OAuthCard because no cached token was found.</summary>
        public const string CardSent = "card_sent";
        /// <summary>GetToken found a cached token in the Token Store.</summary>
        public const string Hit = "hit";
        /// <summary>GetToken found no cached token in the Token Store.</summary>
        public const string Miss = "miss";
        /// <summary>Operation completed successfully.</summary>
        public const string Success = "success";
        /// <summary>Expected protocol failure (e.g., Token Service returned 404/400/412, or null state).</summary>
        public const string Failure = "failure";
        /// <summary>Duplicate signin/tokenExchange invoke; deduplicated to a 200 no-op.</summary>
        public const string Duplicate = "duplicate";
        /// <summary>verify_state attempted on a flow whose connection didn't match the code.</summary>
        public const string NoToken = "no_token";
        /// <summary>signin_failure invoke acknowledged and forwarded to the OnSignInFailure callback.</summary>
        public const string Notified = "notified";
    }

    /// <summary>
    /// Values used for the <see cref="Tags.OAuthErrorType"/> tag.
    /// Set only on the <see cref="OAuthErrors"/> counter and on spans when an unexpected exception escapes.
    /// </summary>
    public static class OAuthErrorTypes
    {
        public const string HttpError = "http_error";
        public const string InvalidOperation = "invalid_op";
        public const string EmptyToken = "empty_token";
    }

    /// <summary>
    /// Names of low-cardinality span events emitted by OAuth flows.
    /// </summary>
    public static class OAuthEvents
    {
        public const string CardSent = "oauth.card.sent";
    }

    /// <summary>
    /// Special <see cref="Tags.OAuthConnection"/> value used by operations that span all connections
    /// (e.g., <c>connection_status</c> returns the status of every registered OAuth connection).
    /// </summary>
    public const string OAuthAllConnections = "all";
}
