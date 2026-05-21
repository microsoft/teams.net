// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using A2A;

namespace A2ABot.A2A;

static class AgentCardFactory
{
    public static AgentCard Build(Config config) => new()
    {
        Name        = config.Name,
        Description = config.Description,
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
                Id          = "handoff",
                Name        = "Handoff",
                Description = $"Accepts handoffs of users from peer bots. Specialty: {config.Description}",
                Tags        = ["a2a", "teams", "handoff"],
            }
        ],
    };
}
