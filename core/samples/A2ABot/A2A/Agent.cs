// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using A2A;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Schema;

namespace A2ABot;

sealed class Agent(State state, Config config, IServiceScopeFactory scopeFactory) : IAgentHandler
{
    public async Task ExecuteAsync(RequestContext context, AgentEventQueue eventQueue, CancellationToken ct)
    {
        var responder = new MessageResponder(eventQueue, context.ContextId);

        Part? dataPart = context.Message.Parts.FirstOrDefault(p => p.Data.HasValue);
        if (dataPart?.Data is not { } data)
        {
            await responder.ReplyAsync("Unrecognised message format.", ct);
            return;
        }

        string? kind = data.TryGetProperty("kind", out var k) ? k.GetString() : null;

        switch (kind)
        {
            case "ask":
                AskMessage? ask = data.Deserialize<AskMessage>();
                if (ask is null) { await responder.ReplyAsync("Malformed ask.", ct); return; }
                await HandleAskAsync(ask, responder, ct);
                break;

            case "reply":
                ReplyMessage? reply = data.Deserialize<ReplyMessage>();
                if (reply is null) { await responder.ReplyAsync("Malformed reply.", ct); return; }
                await HandleReplyAsync(reply, responder, ct);
                break;

            default:
                await responder.ReplyAsync($"Unknown kind: {kind}.", ct);
                break;
        }
    }

    // Peer asks us a question — push an adaptive card to our Teams operator.
    private async Task HandleAskAsync(AskMessage ask, MessageResponder responder, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(state.OperatorConvId))
        {
            await responder.ReplyAsync("No operator registered yet — have a user message this bot first.", ct);
            return;
        }

        state.PendingInbound[ask.Qid] = ask;

        await SendProactiveCardAsync(
            state.OperatorServiceUrl!,
            state.OperatorConvId,
            Cards.AttachmentElement(Cards.AskCardElement(ask.From, ask.Question, ask.Qid)),
            ct);

        await responder.ReplyAsync($"Ask forwarded to {config.Name}'s operator.", ct);
    }

    // Peer replies to one of our earlier asks — push the answer card to the original user.
    private async Task HandleReplyAsync(ReplyMessage reply, MessageResponder responder, CancellationToken ct)
    {
        if (!state.PendingOutbound.TryRemove(reply.Qid, out var pending))
        {
            await responder.ReplyAsync("Unknown question ID.", ct);
            return;
        }

        await SendProactiveCardAsync(
            pending.ServiceUrl,
            pending.ConvId,
            Cards.AttachmentElement(Cards.ReplyCardElement(reply.From, pending.Question, reply.Answer)),
            ct);

        await responder.ReplyAsync("Reply delivered to user.", ct);
    }

    private async Task SendProactiveCardAsync(string serviceUrl, string convId, JsonElement attachments, CancellationToken ct)
    {
        CoreActivity activity = CoreActivity.CreateBuilder()
            .WithServiceUrl(serviceUrl)
            .WithConversation(new Conversation(convId))
            .WithType("message")
            .WithProperty("attachments", attachments)
            .Build();

        using IServiceScope scope = scopeFactory.CreateScope();
        ConversationClient convClient = scope.ServiceProvider.GetRequiredService<ConversationClient>();
        await convClient.SendActivityAsync(activity, cancellationToken: ct);
    }
}
