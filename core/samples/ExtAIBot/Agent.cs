// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using Microsoft.Extensions.AI;
using Microsoft.Teams.Apps;

namespace ExtAIBot;

// Holds the IChatClient, per-conversation history, and the MCP tool set.
// RunAsync drives a single turn: it builds per-turn tools (local + MCP wrapped for citations),
// streams the model response through TeamsStreamingWriter, and returns the full result.
sealed class Agent
{
    private readonly IChatClient _chatClient;
    private readonly McpToolSet _mcpTools;
    private readonly ConcurrentDictionary<string, List<ChatMessage>> _histories = new();

    private const string SystemPrompt = """
        You are a helpful Teams assistant with tool-calling capabilities.

        Always greet new users with a welcome card.

        When a user asks a technical question, use the available Microsoft Learn search tools to find
        relevant documentation. Cite sources inline using [1], [2], etc. when you reference search results.
        Do not add a references list at the end — citations are displayed separately in the UI.
        """;

    public Agent(IChatClient chatClient, McpToolSet mcpTools)
    {
        _chatClient = chatClient;
        _mcpTools = mcpTools;
    }

    public async Task<RunResult> RunAsync(
        string conversationId,
        string userText,
        TeamsStreamingWriter writer,
        CancellationToken cancellationToken)
    {
        List<ChatMessage> history = _histories.GetOrAdd(
            conversationId,
            _ => [new ChatMessage(ChatRole.System, SystemPrompt)]);

        List<object> pendingCards = [];
        CitationCollector citations = new();

        ChatOptions options = new()
        {
            Tools =
            [
                LocalTools.CreateWelcomeCardTool(pendingCards),
                .. _mcpTools.GetTools(citations)
            ]
        };

        history.Add(new ChatMessage(ChatRole.User, userText));
        await writer.SendInformativeUpdateAsync("Thinking…", cancellationToken);

        string fullText = string.Empty;
        await foreach (ChatResponseUpdate update in
            _chatClient.GetStreamingResponseAsync(history, options, cancellationToken))
        {
            if (!string.IsNullOrEmpty(update.Text))
            {
                await writer.AppendResponseAsync(update.Text, cancellationToken);
                fullText += update.Text;
            }
        }

        if (!string.IsNullOrEmpty(fullText))
            history.Add(new ChatMessage(ChatRole.Assistant, fullText));

        return new RunResult(fullText, pendingCards, citations);
    }
}

readonly record struct RunResult(
    string FullText,
    IList<object> PendingCards,
    CitationCollector Citations);
