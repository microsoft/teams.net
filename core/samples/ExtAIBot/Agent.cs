// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Schema;

namespace ExtAIBot;

// Holds the IChatClient, per-conversation history, and the MCP tool set.
// RunAsync drives a single turn: it builds per-turn tools (local + MCP wrapped for citations),
// streams the model response, then runs a dedicated structured-output call to
// generate exactly 2 follow-up suggestions.
internal sealed class Agent
{
    private readonly IChatClient _chatClient;
    private readonly McpToolSetLifetimeService _mcpTools;
    private readonly ILogger<Agent> _logger;
    private readonly ConcurrentDictionary<string, List<ChatMessage>> _histories = new();
    // One lock per conversation so concurrent turns on the same conversation serialize
    // their history mutations (List<ChatMessage> is not thread-safe).
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    private const string SystemPrompt = """
        You are a Teams docs assistant that can search Microsoft Learn (Teams, .NET, Microsoft Graph, Azure)
        and explain bot concepts (streaming, Adaptive Cards, citations, feedback).

        When you use information from a search tool, cite your sources inline using the "citation" value \
        provided in each result (e.g. [1], [2]).
        Do not add a references or sources list at the end of your response — citations are displayed separately in the UI.
        """;

    private const string FollowUpsPrompt = """
        Produce 2 specific prompts the user might want to ask next.

        Output format — read carefully:
        Return ONLY a JSON object INSTANCE, like this:
        {"prompt1": "How do I stream a reply?", "prompt2": "Show me an Adaptive Card example"}

        Each prompt MUST:
        - Be phrased in the first person, as the user would type.
        - Stay under 8 words.

        Pick based on the conversation:
        - If recent turns have substantive content, drill into a concrete topic, API, or
          concept that just came up.
        - Otherwise (e.g. conversation just started, or the last turn is generic),
          suggest prompts that showcase what you can help with based on the MCP tools available.
        """;

    private sealed record FollowUps(
        [property: JsonPropertyName("prompt1")] string Prompt1,
        [property: JsonPropertyName("prompt2")] string Prompt2);

    public Agent(IChatClient chatClient, McpToolSetLifetimeService mcpTools, ILogger<Agent> logger)
    {
        _chatClient = chatClient;
        _mcpTools = mcpTools;
        _logger = logger;
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

        // Serialize turns within a single conversation so concurrent submits
        // (e.g. clarification race) don't interleave history mutations.
        SemaphoreSlim gate = _locks.GetOrAdd(conversationId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            List<object> pendingCards = [];
            CitationCollector citations = new(_logger);
            McpToolSet mcpTools = _mcpTools.Value;

            ChatOptions options = new()
            {
                Tools =
                [
                    LocalTools.CreateClarificationCardTool(pendingCards, _logger),
                    .. mcpTools.GetTools(citations)
                ]
            };

            history.Add(new ChatMessage(ChatRole.User, userText));
            await writer.SendInformativeUpdateAsync("Thinking…", cancellationToken);

            StringBuilder fullText = new();
            await foreach (ChatResponseUpdate update in
                _chatClient.GetStreamingResponseAsync(history, options, cancellationToken))
            {
                if (!string.IsNullOrEmpty(update.Text))
                {
                    await writer.AppendResponseAsync(update.Text, cancellationToken);
                    fullText.Append(update.Text);
                }
            }

            string fullTextStr = fullText.ToString();
            if (fullTextStr.Length > 0)
                history.Add(new ChatMessage(ChatRole.Assistant, fullTextStr));

            List<SuggestedAction> followUpActions = await GenerateFollowUpsAsync(history, cancellationToken);

            return new RunResult(fullTextStr, pendingCards, followUpActions, citations);
        }
        finally
        {
            gate.Release();
        }
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

        if (!response.TryGetResult(out FollowUps? followUps) || followUps is null)
        {
            _logger.LogWarning("Follow-up generation did not return parseable JSON. Raw response: {Text}", response.Text);
            return [];
        }

        return [
            new SuggestedAction(ActionTypes.IMBack, followUps.Prompt1),
            new SuggestedAction(ActionTypes.IMBack, followUps.Prompt2)
        ];
    }
}

internal readonly record struct RunResult(
    string FullText,
    IList<object> PendingCards,
    IList<SuggestedAction> FollowUpActions,
    CitationCollector Citations);
