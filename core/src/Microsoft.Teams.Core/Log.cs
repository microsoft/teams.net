// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using Microsoft.Extensions.Logging;

namespace Microsoft.Teams.Core;

/// <summary>
/// High-performance logging methods generated via the <see cref="LoggerMessageAttribute"/> source generator.
/// </summary>
internal static partial class Log
{
    // ── BotApplication ──────────────────────────────────────────────────

    private static readonly Func<ILogger, string?, string?, Uri?, string?, IDisposable?> ActivityScopeCallback =
        LoggerMessage.DefineScope<string?, string?, Uri?, string?>("ActivityType={ActivityType} ActivityId={ActivityId} ServiceUrl={ServiceUrl} MSCV={MSCV}");

    public static IDisposable? BeginActivityScope(this ILogger logger, string? activityType, string? activityId, Uri? serviceUrl, string? mscv) =>
        ActivityScopeCallback(logger, activityType, activityId, serviceUrl, mscv);

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Started BotApplication listener for AppID:{AppId} with Teams.Core version {SdkVersion}")]
    public static partial void BotStarted(this ILogger logger, string appId, string sdkVersion);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Start processing HTTP request for activity")]
    public static partial void StartProcessingActivity(this ILogger logger);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Activity received: Type={Type} Id={Id} ServiceUrl={ServiceUrl} MSCV={MSCV}")]
    public static partial void ActivityReceived(this ILogger logger, string? type, string? id, Uri? serviceUrl, string? mscv);

    [LoggerMessage(EventId = 4, Level = LogLevel.Trace, Message = "Received activity: \n {Activity}")]
    public static partial void ReceivedActivityJson(this ILogger logger, string activity);

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "Activity processing timed out after {Timeout}: Id={Id}")]
    public static partial void ActivityTimedOut(this ILogger logger, TimeSpan timeout, string? id);

    [LoggerMessage(EventId = 6, Level = LogLevel.Error, Message = "Error processing activity: Id={Id}")]
    public static partial void ActivityProcessingError(this ILogger logger, Exception ex, string? id);

    [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = "Finished processing activity: Id={Id}")]
    public static partial void ActivityProcessingFinished(this ILogger logger, string? id);

    [LoggerMessage(EventId = 8, Level = LogLevel.Debug, Message = "ServiceUrl in activity ({ActivityServiceUrl}) does not match serviceUrl claim ({ClaimServiceUrl}).")]
    public static partial void LogServiceUrlClaimMismatch(this ILogger logger, Uri? activityServiceUrl, string claimServiceUrl);

    // ── ConversationClient ──────────────────────────────────────────────

    [LoggerMessage(EventId = 10, Level = LogLevel.Information, Message = "Truncating conversation ID for 'agents' channel to comply with length restrictions.")]
    public static partial void TruncatingConversationId(this ILogger logger);

    [LoggerMessage(EventId = 11, Level = LogLevel.Trace, Message = "Updating activity at {Url}: {Activity}")]
    public static partial void UpdatingActivity(this ILogger logger, string url, string activity);

    [LoggerMessage(EventId = 12, Level = LogLevel.Trace, Message = "Updating targeted activity at {Url}: {Activity}")]
    public static partial void UpdatingTargetedActivity(this ILogger logger, string url, string activity);

    [LoggerMessage(EventId = 13, Level = LogLevel.Trace, Message = "Creating conversation at {Url} with parameters: {Parameters}")]
    public static partial void CreatingConversation(this ILogger logger, string url, string parameters);

    [LoggerMessage(EventId = 14, Level = LogLevel.Trace, Message = "Sending conversation history to {Url}: {Transcript}")]
    public static partial void SendingConversationHistory(this ILogger logger, string url, string transcript);

    [LoggerMessage(EventId = 15, Level = LogLevel.Trace, Message = "Uploading attachment to {Url}: {AttachmentData}")]
    public static partial void UploadingAttachment(this ILogger logger, string url, string attachmentData);

    // ── BotHttpClient ───────────────────────────────────────────────────

    [LoggerMessage(EventId = 20, Level = LogLevel.Trace, Message = "HTTP {Method} {Url} body: \n {Body}")]
    public static partial void HttpRequestSending(this ILogger logger, HttpMethod method, string url, string? body);

    [LoggerMessage(EventId = 21, Level = LogLevel.Debug, Message = "HTTP {Method} {Url} Response Status {StatusCode}")]
    public static partial void HttpResponseReceived(this ILogger logger, HttpMethod method, string url, int statusCode);

    [LoggerMessage(EventId = 22, Level = LogLevel.Warning, Message = "Resource not found: {Url}")]
    public static partial void ResourceNotFound(this ILogger logger, string url);

    [LoggerMessage(EventId = 23, Level = LogLevel.Warning, Message = "HTTP request error {Method} {Url}\nStatus Code: {StatusCode}\nResponse Headers: {ResponseHeaders}\nResponse Body: {ResponseBody}")]
    public static partial void HttpRequestError(this ILogger logger, HttpMethod method, string url, HttpStatusCode statusCode, string responseHeaders, string responseBody);

    // ── TurnMiddleware ──────────────────────────────────────────────────

    [LoggerMessage(EventId = 30, Level = LogLevel.Debug, Message = "Registered middleware '{Middleware}' (position {Position}).")]
    public static partial void MiddlewareRegistered(this ILogger logger, string middleware, int position);

    [LoggerMessage(EventId = 31, Level = LogLevel.Debug, Message = "Middleware pipeline completed ({Count} middleware(s)).")]
    public static partial void MiddlewarePipelineCompleted(this ILogger logger, int count);

    [LoggerMessage(EventId = 32, Level = LogLevel.Debug, Message = "Executing middleware '{Middleware}' ({Index}/{Count}).")]
    public static partial void MiddlewareExecuting(this ILogger logger, string middleware, int index, int count);

    // ── JwtExtensions ───────────────────────────────────────────────────

    [LoggerMessage(EventId = 50, Level = LogLevel.Trace, Message = "Resolving signing keys from OIDC authority '{Authority}' for issuer '{Issuer}'.")]
    public static partial void ResolvingSigningKeys(this ILogger logger, string authority, string issuer);

    [LoggerMessage(EventId = 51, Level = LogLevel.Trace, Message = "Token validated for scheme: {Scheme}")]
    public static partial void TokenValidated(this ILogger logger, string scheme);

    [LoggerMessage(EventId = 52, Level = LogLevel.Trace, Message = "Incoming token claims:{Claims}")]
    public static partial void IncomingTokenClaims(this ILogger logger, string claims);

    [LoggerMessage(EventId = 53, Level = LogLevel.Warning, Message = "Forbidden for scheme: {Scheme}")]
    public static partial void ForbiddenForScheme(this ILogger logger, string scheme);

    [LoggerMessage(EventId = 54, Level = LogLevel.Error, Message = "JWT authentication failed for scheme {Scheme}: {ExceptionMessage} | token iss={TokenIssuer} aud={TokenAudience} exp={TokenExpiration} sub={TokenSubject} | expected aud={ConfiguredAudience}")]
    public static partial void JwtAuthenticationFailed(this ILogger logger, Exception ex, string scheme, string exceptionMessage, string tokenIssuer, string tokenAudience, string tokenExpiration, string tokenSubject, string configuredAudience);

    [LoggerMessage(EventId = 55, Level = LogLevel.Warning, Message = "DangerouslyAllowUnauthenticatedRequests is enabled for scheme '{SchemeName}'. Configuring bypass authentication with no token validation. This is INSECURE and should only be used for development.")]
    public static partial void BypassAuthenticationConfigured(this ILogger logger, string schemeName);

    [LoggerMessage(EventId = 56, Level = LogLevel.Warning, Message = "Using bypass authentication scheme succeeded for scheme: {Scheme}. This is INSECURE and should only be used for development.")]
    public static partial void BypassAuthenticationSucceeded(this ILogger logger, string scheme);

    [LoggerMessage(EventId = 57, Level = LogLevel.Warning, Message = "Authentication is not configured for scheme '{SchemeName}'. Configure ClientId or enable DangerouslyAllowUnauthenticatedRequests for local development.")]
    public static partial void AuthenticationNotConfigured(this ILogger logger, string schemeName);

    // ── Hosting (UMI inference) ─────────────────────────────────────────

    [LoggerMessage(EventId = 60, Level = LogLevel.Information, Message = "No ClientCredentials configured; treating ClientId '{ClientId}' as a User-Assigned Managed Identity. Bot Framework tokens will be acquired via the IMDS endpoint.")]
    public static partial void InferringUserAssignedManagedIdentity(this ILogger logger, string clientId);
}
