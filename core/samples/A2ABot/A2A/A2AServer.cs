// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using A2A;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core;

namespace A2ABot.A2A;

// Inbound A2A. Parses the DataPart into a HandoffMessage, creates a 1:1
// Teams conversation with the user, asks Agent to seed that conversation's
// session with the handoff context + greeting, then sends the greeting as
// a proactive message.
internal sealed class A2AServer(
    Config config,
    Agent agent,
    ConversationClient conversations,
    ILogger<A2AServer> logger) : IAgentHandler
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public async Task ExecuteAsync(RequestContext context, AgentEventQueue eventQueue, CancellationToken ct)
    {
        MessageResponder responder = new(eventQueue, context.ContextId);

        Part? dataPart = context.Message?.Parts?.FirstOrDefault(p => p.ContentCase == PartContentCase.Data);
        if (dataPart?.Data is not { } data)
        {
            await responder.ReplyAsync("Expected a DataPart in the message.", ct);
            return;
        }

        HandoffMessage? handoff = data.Deserialize<HandoffMessage>(JsonOpts);
        logger.LogInformation(
            "[{Bot}/A2A] received handoff: from={From} user={User} aadId={AadId} tenant={TenantId} serviceUrl={ServiceUrl}",
            config.Name, handoff?.From, handoff?.UserName, handoff?.AadObjectId, handoff?.TenantId, handoff?.ServiceUrl);

        if (handoff is null
            || handoff.Kind != "handoff"
            || string.IsNullOrEmpty(handoff.AadObjectId)
            || string.IsNullOrEmpty(handoff.TenantId)
            || string.IsNullOrEmpty(handoff.ServiceUrl))
        {
            await responder.ReplyAsync("Unsupported or incomplete handoff message.", ct);
            return;
        }

        Uri serviceUrl = new(handoff.ServiceUrl);

        CreateConversationResponse conv = await conversations.CreateConversationAsync(
            new ConversationParameters
            {
                IsGroup = false,
                TenantId = handoff.TenantId,
                Members = [new TeamsConversationAccount { Id = handoff.AadObjectId }],
            },
            serviceUrl,
            cancellationToken: ct);

        string newConvId = conv.Id
            ?? throw new InvalidOperationException("CreateConversation returned no Id.");

        // Run the LLM with the handoff context so the greeting actually
        // answers the question that came in the summary. The LLM's turn is
        // stored in the thread, so subsequent user replies continue naturally.
        string greeting = await agent.GreetWithHandoffAsync(
            newConvId, handoff.From, handoff.UserName, handoff.Summary, ct);

        TeamsActivity proactive = TeamsActivity.CreateBuilder()
            .WithType(TeamsActivityTypes.Message)
            .WithText(greeting)
            .WithServiceUrl(serviceUrl)
            .WithConversation(new TeamsConversation { Id = newConvId })
            .Build();
        SendActivityResponse? sent = await conversations.SendActivityAsync(proactive, cancellationToken: ct);
        logger.LogInformation("[{Bot}/A2A] proactive greeting sent (conv={ConvId}, activityId={ActivityId})",
            config.Name, newConvId, sent?.Id ?? "<none>");

        await responder.ReplyAsync($"Handoff received and {handoff.UserName} contacted directly.", ct);
    }
}
