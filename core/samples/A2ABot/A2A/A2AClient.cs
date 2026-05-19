// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using A2A;

namespace A2ABot.A2A;

// Outbound A2A. Resolves the peer's AgentCard once (so callers can read
// its live description) and ships HandoffMessage payloads as DataParts.
// Knows nothing about Teams, LLMs, or proactive messaging.
sealed class A2AClient(IHttpClientFactory factory, Config config)
{
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private AgentCard? _cachedCard;
    private global::A2A.A2AClient? _cachedClient;

    public async Task<AgentCard> GetPeerCardAsync(CancellationToken ct)
    {
        if (_cachedCard is not null) return _cachedCard;

        await _initLock.WaitAsync(ct);
        try
        {
            if (_cachedCard is not null) return _cachedCard;

            HttpClient http = factory.CreateClient("a2a");
            A2ACardResolver resolver = new(new Uri(config.PeerUrl), http);
            AgentCard card = await resolver.GetAgentCardAsync(ct);

            _cachedCard = card;
            _cachedClient = new global::A2A.A2AClient(new Uri(card.SupportedInterfaces[0].Url), http);
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

        await _cachedClient!.SendMessageAsync(new SendMessageRequest
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
