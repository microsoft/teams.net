// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Hosting;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps;

/// <summary>
/// Context passed to a server function handler registered via
/// <see cref="TeamsBotApplicationBuilder.WithFunction{TResult}"/>.
/// </summary>
public class FunctionContext(TeamsBotApplication botApp, HttpContext httpContext, FunctionRequest request)
{
    private readonly BotApplicationOptions _options =
        httpContext.RequestServices.GetRequiredService<BotApplicationOptions>();

    /// <summary>Gets the bot's application (client) ID.</summary>
    public string? BotId => _options.AppId;

    /// <summary>Gets the Teams bot service URL for proactive messaging.</summary>
    public Uri? ServiceUrl => _options.ServiceUrl;

    /// <summary>Gets the Microsoft Entra tenant ID, extracted from the request auth token.</summary>
    public string? TenantId => httpContext.User.FindFirst("tid")?.Value;

    /// <summary>Gets the Microsoft Entra object ID of the current user, extracted from the request auth token.</summary>
    public string? UserId => httpContext.User.FindFirst("oid")?.Value;

    /// <summary>Gets the name of the current user, extracted from the request auth token.</summary>
    public string? UserName => httpContext.User.FindFirst(ClaimTypes.Name)?.Value;

    /// <summary>Gets the MSAL Entra auth token from the request Authorization header.</summary>
    public string? AuthToken => AuthenticationHeaderValue.TryParse(
        httpContext.Request.Headers.Authorization.FirstOrDefault(), out var header)
        ? header.Parameter
        : null;

    /// <summary>Gets the raw Teams context JSON node from the request body.</summary>
    public JsonNode? TeamsContext => request.Context;

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
/// <see cref="TeamsBotApplicationBuilder.WithFunction{TBody, TResult}"/>.
/// The deserialized request payload is available via <see cref="Data"/>.
/// </summary>
public class FunctionContext<T>(TeamsBotApplication botApp, HttpContext httpContext, FunctionRequest<T> request)
    : FunctionContext(botApp, httpContext, request)
{
    /// <summary>Gets the deserialized request payload.</summary>
    public T? Data => request.Payload;
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
