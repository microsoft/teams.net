// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps;

/// <summary>
/// Context passed to a server function handler registered via
/// <see cref="TeamsBotApplication.WithFunction"/> (no request body).
/// </summary>
public class FunctionContext(TeamsBotApplication botApp, ILogger log)
{
    /// <summary>Gets or sets the bot's application (client) ID./>.</summary>
    public string? BotId { get; set; }

    /// <summary>Gets or sets the Teams bot service URL for proactive messaging.</summary>
    public Uri? ServiceUrl { get; set; }

    /// <summary>Gets or sets the Microsoft Entra tenant ID, extracted from the request auth token.</summary>
    public string? TenantId { get; set; }

    /// <summary>Gets or sets the Microsoft Entra object ID of the current user, extracted from the request auth token.</summary>
    public string? UserId { get; set; }

    /// <summary>Gets or sets the name of the current user, extracted from the request auth token.</summary>
    public string? UserName { get; set; }

    /// <summary>Gets or sets the MSAL Entra auth token from the request Authorization header.</summary>
    public string? AuthToken { get; set; }

    /// <summary>Gets or sets the unique ID for the current app session (X-Teams-App-Session-Id header).</summary>
    public string? AppSessionId { get; set; }

    /// <summary>Gets or sets the developer-defined unique ID for the page (X-Teams-Page-Id header).</summary>
    public string? PageId { get; set; }

    /// <summary>Gets or sets the developer-defined unique ID for the sub-page (X-Teams-Sub-Page-Id header).</summary>
    public string? SubPageId { get; set; }

    /// <summary>Gets or sets the Microsoft Teams channel ID (X-Teams-Channel-Id header).</summary>
    public string? ChannelId { get; set; }

    /// <summary>Gets or sets the Microsoft Teams chat ID (X-Teams-Chat-Id header).</summary>
    public string? ChatId { get; set; }

    /// <summary>Gets or sets the Microsoft Teams meeting ID (X-Teams-Meeting-Id header).</summary>
    public string? MeetingId { get; set; }

    /// <summary>Gets or sets the Microsoft Teams team ID (X-Teams-Team-Id header).</summary>
    public string? TeamId { get; set; }

    /// <summary>Gets or sets the ID of the parent message from which a task module was launched (X-Teams-Message-Id header).</summary>
    public string? MessageId { get; set; }

    /// <summary>
    /// Gets the Teams conversation ID. Resolved from <see cref="ChatId"/> or <see cref="ChannelId"/>.
    /// </summary>
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
}

/// <summary>
/// Context passed to a server function handler registered via
/// <see cref="TeamsBotApplication.WithFunction{TBody}"/>.
/// The deserialized request body is available via <see cref="Data"/>.
/// </summary>
public class FunctionContext<T>(TeamsBotApplication botApp, ILogger log, T data)
    : FunctionContext(botApp, log)
{
    /// <summary>Gets the deserialized request body.</summary>
    public T Data { get; } = data;
}
