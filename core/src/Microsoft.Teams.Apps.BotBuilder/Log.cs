// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

namespace Microsoft.Teams.Apps.BotBuilder;

/// <summary>
/// High-performance logging methods generated via the <see cref="LoggerMessageAttribute"/> source generator.
/// </summary>
internal static partial class Log
{
    // ── KeyedBotAuthenticationHandler ────────────────────────────────────

    [LoggerMessage(EventId = 100, Level = LogLevel.Debug, Message = "Acquiring agentic token for scope '{Scope}' with AppId '{AppId}' and AgentUserId '{AgentUserId}'.")]
    public static partial void AcquiringAgenticToken(this ILogger logger, string scope, string? appId, string? agentUserId);

    [LoggerMessage(EventId = 101, Level = LogLevel.Warning, Message = "Invalid AgenticUserId '{AgenticUserId}'; falling back to app-only token.")]
    public static partial void InvalidAgenticUserId(this ILogger logger, string? agenticUserId);

    [LoggerMessage(EventId = 102, Level = LogLevel.Debug, Message = "Acquiring app-only token for scope: {Scope}")]
    public static partial void AcquiringAppOnlyToken(this ILogger logger, string scope);

    // ── TeamsBotAdapter ─────────────────────────────────────────────────

    [LoggerMessage(EventId = 110, Level = LogLevel.Debug, Message = "Resp from SendActivitiesAsync: {RespId}")]
    public static partial void SendActivitiesResponse(this ILogger logger, string? respId);

    [LoggerMessage(EventId = 111, Level = LogLevel.Trace, Message = "Sending Invoke Response: \n {InvokeResponse} with status: {Status} \n")]
    public static partial void SendingInvokeResponse(this ILogger logger, string invokeResponse, int status);

    [LoggerMessage(EventId = 112, Level = LogLevel.Warning, Message = "HTTP response is null or has started. Cannot write invoke response. ResponseStarted: {ResponseStarted}")]
    public static partial void CannotWriteInvokeResponse(this ILogger logger, bool? responseStarted);

    // ── TeamsBotFrameworkHttpAdapter ─────────────────────────────────────

    [LoggerMessage(EventId = 120, Level = LogLevel.Error, Message = "Error processing activity: Id={Id}. Delegating to OnTurnError.")]
    public static partial void ActivityProcessingErrorDelegating(this ILogger logger, Exception ex, string? id);
}
