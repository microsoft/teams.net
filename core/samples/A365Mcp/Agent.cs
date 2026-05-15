// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.Teams.Core.Schema;
using ModelContextProtocol.Client;

namespace A365Mcp;

/// <summary>
/// Per-turn agent that resolves MCP tools, replays the conversation through an <see cref="IChatClient"/>,
/// and returns the assistant's reply. Conversation history is owned by <see cref="IConversationHistoryStore"/>
/// so this type can safely be registered as a scoped service.
/// </summary>
internal class Agent(
    IChatClient chatClient,
    IMcpClientFactory mcpClientFactory,
    IConversationHistoryStore historyStore,
    IOptions<AgentOptions> options)
{
    private const string SystemPrompt = """
        You are a Teams assistant that can use the MCP Teams tools to send messages to users, channels, and meetings,
        the MCP Mail tools to read and send emails, the MCP Calendar tools to manage calendar events,
        and the MCP Me tools to access user profile information.
        """;

    public async Task<string> RunAsync(
       string conversationId,
       string userText,
       AgenticIdentity? agentic,
       CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(conversationId);
        ArgumentException.ThrowIfNullOrEmpty(agentic?.AgenticAppId);
        ArgumentException.ThrowIfNullOrEmpty(agentic?.AgenticUserId);

        string[] serverUrls = options.Value.McpServerUrls;
        McpClient[] mcpClients = await CreateClientsAsync(serverUrls, agentic, cancellationToken).ConfigureAwait(false);

        try
        {
            var toolLists = await Task.WhenAll(
                mcpClients.Select(c =>
                    c.ListToolsAsync(cancellationToken: cancellationToken).AsTask())).ConfigureAwait(false);

            List<AITool> allTools = [.. toolLists.SelectMany(t => t)];

            List<ChatMessage> history = historyStore.GetOrCreateHistory(
                conversationId,
                () => [new ChatMessage(ChatRole.System, SystemPrompt)]);

            // Serialize turns within a single conversation so concurrent submits
            // (e.g. clarification race) don't interleave history mutations.
            await using IAsyncDisposable gate = await historyStore.AcquireGateAsync(conversationId, cancellationToken).ConfigureAwait(false);

            history.Add(new ChatMessage(ChatRole.User, userText));

            ChatOptions chatOptions = new()
            {
                Tools = allTools
            };

            var chatResponse = await chatClient.GetResponseAsync(history, chatOptions, cancellationToken).ConfigureAwait(false);

            return chatResponse.Text;
        }
        finally
        {
            await DisposeAllAsync(mcpClients).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Creates one MCP client per server URL in parallel. If any creation fails, every
    /// already-created client is disposed before the failure is rethrown so we never leak
    /// transports on partial failure.
    /// </summary>
    private async Task<McpClient[]> CreateClientsAsync(
        IReadOnlyList<string> serverUrls,
        AgenticIdentity agentic,
        CancellationToken cancellationToken)
    {
        Task<McpClient>[] tasks = [.. serverUrls.Select(url => mcpClientFactory.CreateClientAsync(url, agentic, cancellationToken))];
        try
        {
            return await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch
        {
            McpClient[] created = [.. tasks
                .Where(t => t.Status == TaskStatus.RanToCompletion)
                .Select(t => t.Result)];
            await DisposeAllAsync(created).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Best-effort dispose of every client. Disposal failures are swallowed so they cannot
    /// mask the real exception flowing through the surrounding <c>finally</c>.
    /// </summary>
    private static async ValueTask DisposeAllAsync(IReadOnlyList<McpClient> clients)
    {
        foreach (McpClient client in clients)
        {
            try
            {
                await client.DisposeAsync().ConfigureAwait(false);
            }
            catch
            {
                // Ignore: see method summary.
            }
        }
    }
}
