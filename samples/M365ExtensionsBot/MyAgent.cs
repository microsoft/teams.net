// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Agents.Builder;
using Microsoft.Agents.Builder.App;
using Microsoft.Agents.Builder.State;
using Microsoft.Agents.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Clients;
using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core.Schema;
using Microsoft.Teams.M365Extensions;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RouteHandler = Microsoft.Agents.Builder.App.RouteHandler;
using RouteBuilder = Microsoft.Agents.Builder.App.RouteBuilder;

namespace M365ExtensionsBot;

/// <summary>
/// Agent SDK handlers for unmatched Teams turns and every non-Teams channel.
/// </summary>
[Agent(name: "MyAgent", description: "Agent SDK handler with Teams SDK integration", version: "1.0")]
public class MyAgent : AgentApplication
{
    private const string GraphBaseUrl = "https://graph.microsoft.com/v1.0";
    private static readonly string[] AuthHandlerIds = ["graphuser", "graphmail"];

    private readonly MyTeamsBot _teamsBot;
    private readonly IHttpClientFactory _httpClientFactory;
    public MyAgent(
        AgentApplicationOptions options,
        MyTeamsBot teamsBot,
        IHttpClientFactory httpClientFactory,
        ILogger<MyAgent> logger)
        : base(options)
    {
        _teamsBot = teamsBot;
        _httpClientFactory = httpClientFactory;

        AddMessageCommandRoute("whoami", DeclineAuthOnEmailAsync, rank: RouteRank.First, channelPredicate: IsEmailChannel);
        AddMessageCommandRoute("mail", DeclineAuthOnEmailAsync, rank: RouteRank.First, channelPredicate: IsEmailChannel);
        AddMessageCommandRoute("signout", DeclineAuthOnEmailAsync, rank: RouteRank.First, channelPredicate: IsEmailChannel);

        AddMessageCommandRoute("help", HelpAsync);
        AddMessageCommandRoute("channel", ChannelAsync);
        AddMessageCommandRoute("agents sdk react", AgentsSdkReactAsync);
        AddMessageCommandRoute("agents sdk proactive", AgentsSdkProactiveAsync);
        AddMessageCommandRoute("whoami", WhoAmIAsync, autoSignInHandlers: ["graphuser"]);
        AddMessageCommandRoute("mail", MailAsync, autoSignInHandlers: ["graphmail"]);
        AddMessageCommandRoute("signout", SignOutAsync);

        OnActivity(ActivityTypes.Message, EchoAsync, rank: RouteRank.Last);

        UserAuthorization.OnUserSignInFailure(async (turnContext, turnState, handlerName, response, activity, cancellationToken) =>
        {
            string message = response.Error?.Message ?? "unknown error";
            await turnContext.SendActivityAsync(
                $"[Agent SDK] Sign-in failed for '{handlerName}': {response.Cause}/{message}",
                cancellationToken: cancellationToken);
        });
    }

    /// <summary>
    /// Message routed from the Agents SDK uses Teams SDK APIs only on channels that support them.
    /// </summary>
    private async Task AgentsSdkReactAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        if (!TeamsSdkMiddleware.IsTeamsChannel(turnContext.Activity))
        {
            await turnContext.SendActivityAsync(
                $"[Agent SDK] 'agents sdk react' needs the Teams reactions API; channelId={turnContext.Activity.ChannelId} returns 404 for it.",
                cancellationToken: cancellationToken);
            return;
        }

        var response = await turnContext.SendActivityAsync(
            MessageFactory.Text("[Agent SDK] Adding then removing thumbs-up via Teams SDK..."),
            cancellationToken: cancellationToken);

        if (response?.Id != null)
        {
            string conversationId = turnContext.Activity.Conversation.Id;
            var api = _teamsBot.Api.ForServiceUrl(new Uri(turnContext.Activity.ServiceUrl));

            await Task.Delay(2000, cancellationToken);
            await api.Conversations.AddReactionAsync(
                conversationId, response.Id, ReactionTypes.Like, cancellationToken: cancellationToken);

            await Task.Delay(2000, cancellationToken);
            await api.Conversations.DeleteReactionAsync(
                conversationId, response.Id, ReactionTypes.Like, cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Sends a proactive-style message via a per-turn Teams SDK API client.
    /// </summary>
    private async Task AgentsSdkProactiveAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        string conversationId = turnContext.Activity.Conversation.Id;
        var api = _teamsBot.Api.ForServiceUrl(new Uri(turnContext.Activity.ServiceUrl));
        var outgoing = new MessageActivityInput()
            .WithText("[Teams SDK] Proactive message triggered from an Agents SDK handler!");

        await api.Conversations.CreateActivityAsync(conversationId, outgoing, cancellationToken: cancellationToken);
    }

    private async Task HelpAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        await turnContext.SendActivityAsync(
            "[Agent SDK] Commands: help, channel, whoami, mail, signout, agents sdk react, agents sdk proactive.\n" +
            "Teams-only extras (react, quote, targeted, task) need the Teams SDK routes.",
            cancellationToken: cancellationToken);
    }

    private async Task ChannelAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        string via = TeamsSdkMiddleware.IsTeamsChannel(turnContext.Activity)
            ? "Teams turn with no matching Teams SDK route -> fell through"
            : "non-Teams channel -> passed straight through";

        await turnContext.SendActivityAsync(
            $"[Agent SDK] channelId={turnContext.Activity.ChannelId} ({via})",
            cancellationToken: cancellationToken);
    }

    private async Task WhoAmIAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        JsonElement? me = await GraphGetAsync(turnContext, "graphuser", "/me", cancellationToken);
        if (me is null)
        {
            return;
        }

        string displayName = me.Value.TryGetProperty("displayName", out JsonElement displayNameValue)
            ? displayNameValue.GetString() ?? "(unknown user)"
            : "(unknown user)";
        string userPrincipalName = me.Value.TryGetProperty("userPrincipalName", out JsonElement upnValue)
            ? upnValue.GetString() ?? "(no upn)"
            : "(no upn)";

        await turnContext.SendActivityAsync(
            $"[Agent SDK] Signed in via 'graphuser'.\n[Agent SDK] {displayName} ({userPrincipalName})\nHandler 'graphuser' - scope User.Read.",
            cancellationToken: cancellationToken);
    }

    private async Task MailAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        JsonElement? payload = await GraphGetAsync(
            turnContext,
            "graphmail",
            "/me/messages?$top=3&$select=subject,receivedDateTime",
            cancellationToken);
        if (payload is null)
        {
            return;
        }

        if (!payload.Value.TryGetProperty("value", out JsonElement messages) || messages.GetArrayLength() == 0)
        {
            await turnContext.SendActivityAsync("[Agent SDK] Mailbox is empty.", cancellationToken: cancellationToken);
            return;
        }

        List<string> lines = [];
        foreach (JsonElement message in messages.EnumerateArray())
        {
            string subject = message.TryGetProperty("subject", out JsonElement subjectValue)
                ? subjectValue.GetString() ?? "(no subject)"
                : "(no subject)";
            lines.Add($"- {subject}");
        }

        await turnContext.SendActivityAsync(
            $"[Agent SDK] Signed in via 'graphmail'.\n[Agent SDK] Latest {lines.Count} message(s):\n{string.Join('\n', lines)}\nHandler 'graphmail' - scopes User.Read + Mail.Read.",
            cancellationToken: cancellationToken);
    }

    private async Task SignOutAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        foreach (string handlerId in AuthHandlerIds)
        {
            await UserAuthorization.SignOutUserAsync(turnContext, turnState, handlerId, cancellationToken: cancellationToken);
        }

        await turnContext.SendActivityAsync(
            $"[Agent SDK] Signed out of: {string.Join(", ", AuthHandlerIds)}.",
            cancellationToken: cancellationToken);
    }

    private async Task DeclineAuthOnEmailAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        await turnContext.SendActivityAsync(
            $"[Agent SDK] Sign-in isn't supported on {turnContext.Activity.ChannelId} - the OAuth card renders as a static image here, so it can't be clicked. Tokens are scoped per channel, so there is nothing to sign in or out of on this one. Try whoami / mail on Teams or Web Chat.",
            cancellationToken: cancellationToken);
    }

    private async Task EchoAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        string text = (turnContext.Activity.Text ?? string.Empty).Trim();
        string firstLine = GetFirstNonEmptyLine(text);
        if (!string.Equals(firstLine, text, StringComparison.Ordinal))
        {
            text = $"{firstLine} [...]";
        }

        await turnContext.SendActivityAsync(
            $"[Agent SDK] ({turnContext.Activity.ChannelId}) You said: {text}",
            cancellationToken: cancellationToken);
    }

    private async Task<JsonElement?> GraphGetAsync(ITurnContext turnContext, string handlerName, string resource, CancellationToken cancellationToken)
    {
        string? token = await turnContext.GetTurnTokenAsync(handlerName, cancellationToken);
        if (string.IsNullOrEmpty(token))
        {
            await turnContext.SendActivityAsync(
                $"[Agent SDK] No token for the '{handlerName}' handler.",
                cancellationToken: cancellationToken);
            return null;
        }

        using HttpRequestMessage request = new(HttpMethod.Get, $"{GraphBaseUrl}{resource}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpClient client = _httpClientFactory.CreateClient();
        HttpResponseMessage response = await client.SendAsync(request, cancellationToken);
        string content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string detail = content;
            try
            {
                using JsonDocument errorDocument = JsonDocument.Parse(content);
                if (errorDocument.RootElement.TryGetProperty("error", out JsonElement errorElement)
                    && errorElement.TryGetProperty("message", out JsonElement messageElement)
                    && messageElement.GetString() is string graphMessage
                    && !string.IsNullOrWhiteSpace(graphMessage))
                {
                    detail = graphMessage;
                }
            }
            catch (JsonException)
            {
                // Keep the raw response text when Graph does not return JSON.
            }

            await turnContext.SendActivityAsync(
                $"[Agent SDK] Graph {resource} returned {(int)response.StatusCode}: {detail}",
                cancellationToken: cancellationToken);
            return null;
        }

        using JsonDocument document = JsonDocument.Parse(content);
        return document.RootElement.Clone();
    }

    private void AddMessageCommandRoute(
        string command,
        RouteHandler handler,
        ushort rank = RouteRank.Unspecified,
        string[]? autoSignInHandlers = null,
        Func<ITurnContext, bool>? channelPredicate = null)
    {
        var builder = RouteBuilder.Create()
            .WithSelector((turnContext, cancellationToken) => Task.FromResult(
                turnContext.Activity.Type == ActivityTypes.Message
                && (channelPredicate?.Invoke(turnContext) ?? true)
                && IsCommand(turnContext, command)))
            .WithHandler(handler)
            .WithOrderRank(rank);

        if (autoSignInHandlers is not null)
        {
            builder.WithOAuthHandlers(autoSignInHandlers);
        }

        AddRoute(builder.Build());
    }

    private static bool IsCommand(ITurnContext turnContext, string command)
        => string.Equals(GetCommandText(turnContext), command, StringComparison.OrdinalIgnoreCase);

    private static string GetCommandText(ITurnContext turnContext)
    {
        string? text = turnContext.Activity.Text;
        if (TeamsSdkMiddleware.IsTeamsChannel(turnContext.Activity))
        {
            text = turnContext.Activity.RemoveRecipientMention();
        }

        return GetFirstNonEmptyLine(text ?? string.Empty);
    }

    private static string GetFirstNonEmptyLine(string text)
        => text
            .Split(["\r\n", "\n"], StringSplitOptions.None)
            .Select(line => line.Trim())
            .FirstOrDefault(line => !string.IsNullOrEmpty(line))
            ?? string.Empty;

    private static bool IsEmailChannel(ITurnContext turnContext)
        => string.Equals(turnContext.Activity.ChannelId?.ToString(), Channels.Email.ToString(), StringComparison.OrdinalIgnoreCase);
}
