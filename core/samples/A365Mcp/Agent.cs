// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using Microsoft.Extensions.AI;
using Microsoft.Teams.Core.Schema;
using ModelContextProtocol.Client;

namespace A365Mcp;

internal class Agent
{
    private readonly IChatClient _chatClient;
    private readonly IMcpClientFactory _mcpClientFactory;
    private readonly ILogger<Agent> _logger;
    private readonly ConcurrentDictionary<string, List<ChatMessage>> _histories = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public Agent(IChatClient chatClient, IMcpClientFactory mcpClientFactory, ILogger<Agent> logger)
    {
        _chatClient = chatClient;
        _mcpClientFactory = mcpClientFactory;
        _logger = logger;
    }

    private const string SystemPrompt = """
        You are a Teams assistant that can use the MCP Teams tools to send messages to users, channels, and meetings.
        """;

    public async Task<string> RunAsync(
       string conversationId,
       string userText,
       AgenticIdentity? agentic,
       CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(agentic?.AgenticAppId);
        ArgumentNullException.ThrowIfNullOrEmpty(agentic?.AgenticUserId);

        await using var teamsMcpClient = await _mcpClientFactory.CreateClientAsync(agentic, cancellationToken).ConfigureAwait(false);
        var teamsMcpTools = await teamsMcpClient.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        List<ChatMessage> history = _histories.GetOrAdd(conversationId, _ => [new ChatMessage(ChatRole.System, SystemPrompt)]);

        // Serialize turns within a single conversation so concurrent submits
        // (e.g. clarification race) don't interleave history mutations.
        SemaphoreSlim gate = _locks.GetOrAdd(conversationId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            history.Add(new ChatMessage(ChatRole.User, userText));

            ChatOptions options = new()
            {
                Tools = [.. teamsMcpTools]
            };

            var chatResponse = await _chatClient.GetResponseAsync(history, options, cancellationToken).ConfigureAwait(false);

            return chatResponse.Text;
        }
        finally
        {
            gate.Release();
        }
    }
}
