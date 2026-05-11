// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using Microsoft.Extensions.AI;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Schema;

namespace ExtAIBot;

// Holds the IChatClient, per-conversation history, and the MCP tool set.
// RunAsync drives a single turn: it builds per-turn tools (local + MCP wrapped for citations),
// streams the model response, then runs a dedicated structured-output call to
// generate exactly 2 follow-up suggestions.
sealed class Agent
{
    private readonly IChatClient _chatClient;
    private readonly McpToolSet _mcpTools;
    private readonly ConcurrentDictionary<string, List<ChatMessage>> _histories = new();

    private const string SystemPrompt = """
        You are a helpful Teams assistant with tool-calling capabilities.

        When you use information from a search tool, cite your sources inline using the "citation" value \
        provided in each result (e.g. [1], [2]).
        Do not add a references or sources list at the end of your response — citations are displayed separately in the UI.
        """;

    private const string FollowUpsPrompt = """
        Given the conversation above, produce 2 specific follow-up prompts the
        user might want to ask next.

        Each prompt MUST:
        - Drill into a concrete topic or concept from the recent history.
        - Be a natural next question a curious user would type after reading it.
        - Be phrased in the first person, as the user would type.
        - Stay under 8 words.
        """;

    private sealed record FollowUps(string Prompt1, string Prompt2);

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
                LocalTools.CreateClarificationCardTool(pendingCards),
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

        List<SuggestedAction> followUpActions = await GenerateFollowUpsAsync(history, cancellationToken);

        return new RunResult(fullText, pendingCards, followUpActions, citations);
    }

    // Runs after the streamed reply is in history. Forces structured JSON output matching
    // the FollowUps shape so we always get exactly 2 suggestions to display as chips.
    private async Task<List<SuggestedAction>> GenerateFollowUpsAsync(
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken)
    {
        List<ChatMessage> messages =
        [
            .. history,
            new ChatMessage(ChatRole.System, FollowUpsPrompt)
        ];

        ChatResponse<FollowUps> response = await _chatClient.GetResponseAsync<FollowUps>(
            messages,
            cancellationToken: cancellationToken);

        return response.TryGetResult(out FollowUps? followUps) && followUps is not null
            ? [new SuggestedAction(ActionType.IMBack, followUps.Prompt1),
               new SuggestedAction(ActionType.IMBack, followUps.Prompt2)]
            : [];
    }
}

readonly record struct RunResult(
    string FullText,
    IList<object> PendingCards,
    IList<SuggestedAction> FollowUpActions,
    CitationCollector Citations);
