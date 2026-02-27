// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps;

/// <summary>
/// Context passed to a server function handler registered via
/// <see cref="TeamsBotApplicationBuilder.WithFunction{TResult}"/>.
/// </summary>
public class FunctionContext(TeamsBotApplication botApp)
{
    /// <summary>Gets the bot's application (client) ID.</summary>
    public string BotId => botApp.Options.AppId;

    /// <summary>Gets the Teams bot service URL for proactive messaging.</summary>
    public Uri ServiceUrl => botApp.Options.ServiceUrl;

    /// <summary>Gets the Microsoft Entra tenant ID, extracted from the request auth token.</summary>
    public string? TenantId { get; init; }

    /// <summary>Gets the Microsoft Entra object ID of the current user, extracted from the request auth token.</summary>
    public string? UserId { get; init; }

    /// <summary>Gets the name of the current user, extracted from the request auth token.</summary>
    public string? UserName { get; init; }

    /// <summary>Gets the MSAL Entra auth token from the request Authorization header.</summary>
    public string? AuthToken { get; init; }

    //TODO : review if we should parse out more fields from the Teams context and make them first-class properties (e.g. chat vs channel, team id, etc.)
    /// <summary>Gets the raw Teams context JSON node from the request body.</summary>
    public JsonNode? TeamsContext { get; init; }

    /// <summary>Teams conversation ID, resolved after a call to <see cref="SendAsync"/>.</summary>
    public string? ConversationId { get; private set; }

    /// <summary>
    /// Sends a text message to the conversation proactively.
    /// </summary>
    public async Task<SendActivityResponse> SendAsync(string message, CancellationToken cancellationToken = default)
    {
        var conversationId = TeamsContext?["chat"]?["id"]?.GetValue<string>()
            ?? TeamsContext?["channel"]?["id"]?.GetValue<string>()
            ?? ConversationId;

        // Conversation ID can be missing if the app is running in a personal scope. In this case, create
        // a conversation between the bot and the user. This will either create a new conversation or return
        // a pre-existing one.
        if (conversationId is null)
        {
            if (ServiceUrl is null)
                throw new InvalidOperationException("ServiceUrl is not configured. Set BotOptions.ServiceUrl to send proactive messages.");

            var res = await botApp.ConversationClient.CreateConversationAsync(new ConversationParameters
            {
                TenantId = TenantId,
                IsGroup = false,
                Bot = new ConversationAccount { Id = BotId },
                Members = [new ConversationAccount { Id = UserId }]
            }, ServiceUrl, cancellationToken: cancellationToken).ConfigureAwait(false);

            conversationId = res.Id;
        }

        MessageActivity activity = new(message) { ServiceUrl = ServiceUrl };
        activity.Conversation.Id = conversationId!;
        ConversationId = conversationId;

        return await botApp.ConversationClient.SendActivityAsync(activity, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Context passed to a server function handler registered via
/// <see cref="TeamsBotApplicationBuilder.WithFunction{TBody}"/>.
/// The deserialized request payload is available via <see cref="Data"/>.
/// </summary>
public class FunctionContext<T>(TeamsBotApplication botApp) : FunctionContext(botApp)
{
    /// <summary>Gets the deserialized request payload.</summary>
    public T? Data { get; init; }
}

/// <summary>
/// Represents the JSON body sent by the Teams client to a server function endpoint.
/// </summary>
public class FunctionRequest
{
    /// <summary>Gets or sets the raw Teams context JSON node.</summary>
    public JsonNode? Context { get; set; }
}

/// <summary>
/// Represents the JSON body sent by the Teams client to a server function endpoint,
/// with a typed <typeparamref name="T"/> payload.
/// </summary>
/// <typeparam name="T">The type to deserialize the request payload into.</typeparam>
public sealed class FunctionRequest<T> : FunctionRequest
{
    /// <summary>Gets or sets the deserialized request payload.</summary>
    public T? Payload { get; set; }
}
