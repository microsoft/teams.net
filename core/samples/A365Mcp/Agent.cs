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
        You are a Teams assistant that can use the MCP Teams tools to send messages to users, channels, and meetings,
        the MCP Mail tools to read and send emails, the MCP Calendar tools to manage calendar events,
        and the MCP Me tools to access user profile information.
        """;

    private static readonly string[] McpServerUrls =
    [
        "https://agent365.svc.cloud.microsoft/agents/servers/mcp_TeamsServer",
        "https://agent365.svc.cloud.microsoft/agents/servers/mcp_MailTools",
        "https://agent365.svc.cloud.microsoft/agents/servers/mcp_CalendarTools",
        "https://agent365.svc.cloud.microsoft/agents/servers/mcp_MeServer",
    ];

    public async Task<string> RunAsync(
       string conversationId,
       string userText,
       AgenticIdentity? agentic,
       CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(agentic?.AgenticAppId);
        ArgumentNullException.ThrowIfNullOrEmpty(agentic?.AgenticUserId);

        var mcpClients = await Task.WhenAll(
            McpServerUrls.Select(url => _mcpClientFactory.CreateClientAsync(url, agentic, cancellationToken)))
            .ConfigureAwait(false);

        try
        {
            var toolLists = await Task.WhenAll(
                mcpClients.Select(c => c.ListToolsAsync(cancellationToken: cancellationToken).AsTask()))
                .ConfigureAwait(false);

            var allTools = toolLists.SelectMany(t => t).ToList();

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
                    Tools = [.. allTools]
                };

                var chatResponse = await _chatClient.GetResponseAsync(history, options, cancellationToken).ConfigureAwait(false);

                return chatResponse.Text;
            }
            finally
            {
                gate.Release();
            }
        }
        finally
        {
            foreach (var client in mcpClients)
            {
                await client.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
