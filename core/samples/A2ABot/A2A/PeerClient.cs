// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using A2A;

namespace A2ABot;

sealed class PeerClient(IHttpClientFactory factory)
{
    public Task SendAskAsync(string peerBaseUrl, AskMessage ask, CancellationToken ct = default)
        => SendDataPartAsync(peerBaseUrl, ask, ct);

    public Task SendReplyAsync(string replyBaseUrl, ReplyMessage reply, CancellationToken ct = default)
        => SendDataPartAsync(replyBaseUrl, reply, ct);

    private async Task SendDataPartAsync<T>(string baseUrl, T payload, CancellationToken ct)
    {
        HttpClient http = factory.CreateClient("a2a");

        // Discover the peer's A2A endpoint from its well-known agent card.
        A2ACardResolver resolver = new(new Uri(baseUrl), http);
        AgentCard card = await resolver.GetAgentCardAsync(ct);

        A2AClient client = new(new Uri(card.SupportedInterfaces[0].Url), http);

        await client.SendMessageAsync(new SendMessageRequest
        {
            Message = new Message
            {
                MessageId = Guid.NewGuid().ToString("N"),
                Role = Role.User,
                Parts = [Part.FromData(JsonSerializer.SerializeToElement(payload))]
            }
        }, ct);
    }
}
