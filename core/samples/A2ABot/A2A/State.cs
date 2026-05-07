// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;

namespace A2ABot;

sealed class State
{
    // Teams conversation reference captured from first user message.
    public string? OperatorConvId { get; set; }
    public string? OperatorServiceUrl { get; set; }

    // Asks we sent to peer, waiting for a reply. Key = qid.
    public ConcurrentDictionary<string, (string ConvId, string ServiceUrl, string Question)> PendingOutbound { get; } = new();

    // Asks received from peer, waiting for our operator to answer. Key = qid.
    public ConcurrentDictionary<string, AskMessage> PendingInbound { get; } = new();
}
