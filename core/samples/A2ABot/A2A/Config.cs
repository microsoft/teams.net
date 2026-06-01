// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace A2ABot.A2A;

// Description goes into this bot's A2A AgentCard — the peer's LLM reads
// it to decide whether to hand off to us.
internal record Config(
    string Name,
    string SelfUrl,
    string Description,
    string PeerUrl,
    string PeerName);
