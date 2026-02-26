// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Hosting;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps;

/// <summary>
/// Context passed to a server function handler registered via
/// <see cref="TeamsBotApplicationBuilder.WithFunction"/>.
/// </summary>
public class FunctionContext(TeamsBotApplication botApp, ILogger log, HttpContext httpContext, BotApplicationOptions options)
{
    /// <summary>Gets the bot's application (client) ID.</summary>
    public string? BotId => options.AppId;

    /// <summary>Gets the Teams bot service URL for proactive messaging.</summary>
    public Uri? ServiceUrl => options.ServiceUrl;

    /// <summary>Gets the Microsoft Entra tenant ID, extracted from the request auth token.</summary>
    public string? TenantId => httpContext.User.FindFirst("tid")?.Value;

    /// <summary>Gets the Microsoft Entra object ID of the current user, extracted from the request auth token.</summary>
    public string? UserId => httpContext.User.FindFirst("oid")?.Value;

    /// <summary>Gets the name of the current user, extracted from the request auth token.</summary>
    public string? UserName => httpContext.User.FindFirst(ClaimTypes.Name)?.Value;

    /// <summary>Gets the MSAL Entra auth token from the request Authorization header.</summary>
    public string? AuthToken => httpContext.Request.Headers.Authorization.FirstOrDefault()
        ?.Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase);

    /// <summary>Gets the unique ID for the current app session (X-Teams-App-Session-Id header).</summary>
    public string? AppSessionId => GetHeader("X-Teams-App-Session-Id");

    /// <summary>Gets the developer-defined unique ID for the page (X-Teams-Page-Id header).</summary>
    public string? PageId => GetHeader("X-Teams-Page-Id");

    /// <summary>Gets the developer-defined unique ID for the sub-page (X-Teams-Sub-Page-Id header).</summary>
    public string? SubPageId => GetHeader("X-Teams-Sub-Page-Id");

    /// <summary>Gets the Microsoft Teams channel ID (X-Teams-Channel-Id header).</summary>
    public string? ChannelId => GetHeader("X-Teams-Channel-Id");

    /// <summary>Gets the Microsoft Teams chat ID (X-Teams-Chat-Id header).</summary>
    public string? ChatId => GetHeader("X-Teams-Chat-Id");

    /// <summary>Gets the Microsoft Teams meeting ID (X-Teams-Meeting-Id header).</summary>
    public string? MeetingId => GetHeader("X-Teams-Meeting-Id");

    /// <summary>Gets the Microsoft Teams team ID (X-Teams-Team-Id header).</summary>
    public string? TeamId => GetHeader("X-Teams-Team-Id");

    /// <summary>Gets the ID of the parent message from which a task module was launched (X-Teams-Message-Id header).</summary>
    public string? MessageId => GetHeader("X-Teams-Message-Id");

    /// <summary>Gets the Teams conversation ID. Resolved from <see cref="ChatId"/> or <see cref="ChannelId"/>.</summary>
    public string? ConversationId => ChatId ?? ChannelId;

    /// <summary>Gets the logger for this function.</summary>
    public ILogger Log { get; } = log;

    /// <summary>
    /// Sends a text message to the conversation proactively.
    /// </summary>
    public async Task<SendActivityResponse> SendAsync(string message, CancellationToken cancellationToken = default)
    {
        var conversationId = ConversationId;

        // Conversation ID can be missing if the app is running in a personal scope. In this case, create
        // a conversation between the bot and the user. This will either create a new conversation or return
        // a pre-existing one.
        if (conversationId is null)
        {
            var res = await botApp.ConversationClient.CreateConversationAsync(new ConversationParameters
            {
                TenantId = TenantId,
                IsGroup = false,
                Bot = new ConversationAccount { Id = BotId },
                Members = [new ConversationAccount { Id = UserId }]
            }, ServiceUrl!, cancellationToken: cancellationToken).ConfigureAwait(false);

            conversationId = res.Id;
        }

        var activity = new MessageActivity(message) { ServiceUrl = ServiceUrl };
        activity.Conversation.Id = conversationId!;

        return await botApp.ConversationClient.SendActivityAsync(activity, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private string? GetHeader(string name) =>
        httpContext.Request.Headers.TryGetValue(name, out var value) ? (string?)value : null;
}

/// <summary>
/// Context passed to a server function handler registered via
/// <see cref="TeamsBotApplicationBuilder.WithFunction{TBody}"/>.
/// The deserialized request body is available via <see cref="Data"/>.
/// </summary>
public class FunctionContext<T>(TeamsBotApplication botApp, ILogger log, HttpContext httpContext, BotApplicationOptions options, T data)
    : FunctionContext(botApp, log, httpContext, options)
{
    /// <summary>Gets the deserialized request body.</summary>
    public T Data { get; } = data;
}
