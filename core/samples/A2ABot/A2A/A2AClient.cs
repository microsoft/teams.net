// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using A2A;

namespace A2ABot.A2A;

// Outbound A2A. Resolves the peer's AgentCard once (so callers can read
// its live description) and ships HandoffMessage payloads as DataParts.
// Knows nothing about Teams, LLMs, or proactive messaging.
internal sealed class A2AClient(IHttpClientFactory factory, Config config)
{
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private volatile CachedPeer? _cached;

    private sealed record CachedPeer(AgentCard Card, global::A2A.A2AClient Client);

    public async Task<AgentCard> GetPeerCardAsync(CancellationToken ct)
    {
        if (_cached is not null) return _cached.Card;

        await _initLock.WaitAsync(ct);
        try
        {
            if (_cached is not null) return _cached.Card;

            HttpClient http = factory.CreateClient("a2a");
            A2ACardResolver resolver = new(new Uri(config.PeerUrl), http);
            AgentCard card = await resolver.GetAgentCardAsync(ct);

            global::A2A.A2AClient client = new(new Uri(card.SupportedInterfaces[0].Url), http);
            _cached = new CachedPeer(card, client);
            return card;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task SendHandoffAsync(HandoffMessage payload, CancellationToken ct)
    {
        await GetPeerCardAsync(ct);

        await _cached!.Client.SendMessageAsync(new SendMessageRequest
        {
            Message = new Message
            {
                MessageId = Guid.NewGuid().ToString("N"),
                Role = Role.User,
                Parts = [Part.FromData(JsonSerializer.SerializeToElement(payload))],
            }
        }, ct);
    }
}
