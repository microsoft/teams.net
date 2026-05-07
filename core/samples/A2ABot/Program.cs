// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using A2A;
using A2A.AspNetCore;
using A2ABot;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Handlers;

// ── Configuration ─────────────────────────────────────────────────────────────
WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(args);
builder.Services.AddTeamsBotApplication();
builder.Services.AddHttpClient("a2a");
builder.Services.AddSingleton<State>();
builder.Services.AddSingleton<PeerClient>();

Config config = new(
    Name:     builder.Configuration["Bot:Name"]    ?? "Alice",
    SelfUrl:  builder.Configuration["Bot:SelfUrl"] ?? "http://localhost:3978",
    PeerUrl:  builder.Configuration["Bot:PeerUrl"] ?? string.Empty);

builder.Services.AddSingleton(config);

// Register the A2A agent — exposes a standard A2A endpoint for bot-to-bot messages.
AgentCard agentCard = new()
{
    Name        = config.Name,
    Description = builder.Configuration["Bot:Description"] ?? $"{config.Name} Teams bot",
    Version     = "1.0.0",
    SupportedInterfaces =
    [
        new AgentInterface
        {
            Url             = $"{config.SelfUrl}/a2a",
            ProtocolBinding = "JSONRPC",
            ProtocolVersion = "1.0",
        }
    ],
    DefaultInputModes  = ["application/json"],
    DefaultOutputModes = ["text/plain"],
    Capabilities = new AgentCapabilities { Streaming = false },
    Skills =
    [
        new AgentSkill
        {
            Id          = "ask-reply",
            Name        = "Ask / Reply",
            Description = "Accepts ask and reply messages from peer bots.",
            Tags        = ["a2a", "teams"],
        }
    ],
};

builder.Services.AddA2AAgent<Agent>(agentCard);

WebApplication webApp = builder.Build();

State state          = webApp.Services.GetRequiredService<State>();
PeerClient peerClient = webApp.Services.GetRequiredService<PeerClient>();

TeamsBotApplication teamsApp = webApp.UseTeamsBotApplication();

// ── Teams: handle incoming user messages ──────────────────────────────────────
teamsApp.OnMessage(async (context, cancellationToken) =>
{
    // Remember the operator's conversation reference on first contact.
    state.OperatorConvId     ??= context.Activity.Conversation?.Id;
    state.OperatorServiceUrl ??= context.Activity.ServiceUrl?.ToString();

    string text     = context.Activity.Text?.Trim() ?? string.Empty;
    string userName = context.Activity.From?.Name ?? "User";

    // Messages ending with '?' are forwarded to the peer bot via A2A.
    if (!string.IsNullOrEmpty(config.PeerUrl) && text.EndsWith('?'))
    {
        string qid = Guid.NewGuid().ToString("N")[..8];

        state.PendingOutbound[qid] = (
            ConvId:     context.Activity.Conversation!.Id!,
            ServiceUrl: context.Activity.ServiceUrl!.ToString(),
            Question:   text);

        try
        {
            await peerClient.SendAskAsync(
                config.PeerUrl,
                new AskMessage(qid, text, $"{config.Name} ({userName})", config.SelfUrl),
                cancellationToken);

            await context.SendActivityAsync("_Forwarded to peer bot via A2A. You'll receive a reply shortly._", cancellationToken);
        }
        catch (Exception ex)
        {
            state.PendingOutbound.TryRemove(qid, out _);
            await context.SendActivityAsync($"Could not reach peer bot: {ex.Message}", cancellationToken);
        }

        return;
    }

    await context.SendActivityAsync(
        $"**{config.Name}:** {text}\n\n_Tip: End your message with `?` to forward it to the peer bot via A2A._",
        cancellationToken);
});

// ── Teams: operator submits a reply card ──────────────────────────────────────
teamsApp.OnAdaptiveCardAction(async (context, cancellationToken) =>
{
    if (context.Activity.Value?.Action?.Verb != "a2a-reply")
        return AdaptiveCardResponse.CreateMessageResponse("Unknown action.");

    string? qid    = context.Activity.Value?.Action?.Data?["qid"]?.ToString();
    string? answer = context.Activity.Value?.Action?.Data?["answer"]?.ToString();

    if (string.IsNullOrWhiteSpace(qid) || string.IsNullOrWhiteSpace(answer))
        return AdaptiveCardResponse.CreateMessageResponse("Please provide an answer before submitting.");

    if (!state.PendingInbound.TryRemove(qid, out AskMessage? ask))
        return AdaptiveCardResponse.CreateMessageResponse("Question not found — it may have already been answered.");

    try
    {
        // Reply via A2A back to the asking bot.
        await peerClient.SendReplyAsync(
            ask.ReplyBaseUrl,
            new ReplyMessage(qid, answer, config.Name),
            cancellationToken);

        return AdaptiveCardResponse.CreateMessageResponse($"Reply sent back to {ask.From} via A2A.");
    }
    catch (Exception ex)
    {
        state.PendingInbound[qid] = ask; // restore so operator can retry
        return AdaptiveCardResponse.CreateMessageResponse($"Failed to send reply: {ex.Message}");
    }
});

// ── A2A: expose the standard A2A endpoint and well-known agent card ───────────
webApp.MapA2A("/a2a");
webApp.MapWellKnownAgentCard(agentCard);

webApp.Run();
