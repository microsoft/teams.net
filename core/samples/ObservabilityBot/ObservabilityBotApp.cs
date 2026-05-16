// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Agents.A365.Observability.Runtime.Tracing.Contracts;
using Microsoft.Agents.A365.Observability.Runtime.Tracing.Scopes;
using Microsoft.Extensions.AI;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Api.Clients;
using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.Schema.Entities;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Hosting;

namespace ObservabilityBot;

public class ObservabilityBotApp : TeamsBotApplication
{
    private readonly IChatClient _chatClient;
    private readonly ChatOptions _chatOptions;
    private readonly ConcurrentDictionary<string, List<ChatMessage>> _chatHistories = new();
    private readonly string _deploymentName;

    public ObservabilityBotApp(
        ConversationClient conversationClient,
        UserTokenClient userTokenClient,
        ApiClient teamsApiClient,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ObservabilityBotApp> logger,
        IChatClient chatClient,
        ChatOptions chatOptions,
        BotApplicationOptions? options = null,
        TeamsBotApplicationOptions? teamsOptions = null)
        : base(conversationClient, userTokenClient, teamsApiClient, httpContextAccessor, logger, options, teamsOptions)
    {
        _chatClient = chatClient;
        _chatOptions = chatOptions;
        _deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT") ?? "unknown";

        this.OnMessage(HandleMessageAsync);
    }

    private async Task HandleMessageAsync(Context<MessageActivity> context, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(context.Activity);
        ArgumentNullException.ThrowIfNull(context.Activity.Conversation);
        ArgumentNullException.ThrowIfNull(context.Activity.Conversation.Id);

        await context.Typing(string.Empty, ct);

        var conversationId = context.Activity.Conversation.Id;
        var history = _chatHistories.GetOrAdd(conversationId, _ => []);

        lock (history)
        {
            history.Add(new ChatMessage(ChatRole.User, context.Activity.Text));
        }

        // Build Agent365 scope contracts from the turn context.
        var recipient = context.Activity.Recipient;
        var agentDetails = new AgentDetails(
            agentId: recipient?.AgenticAppId ?? recipient?.Id,
            agentName: recipient?.Name,
            agenticUserId: recipient?.AgenticUserId,
            agentBlueprintId: recipient?.AgenticAppBlueprintId,
            tenantId: recipient?.TenantId);

        var request = new Request(
            content: context.Activity.Text,
            conversationId: conversationId,
            channel: new Channel(context.Activity.ChannelId));

        // === InferenceScope: wraps the LLM + tool-call loop ===
        var inferenceDetails = new InferenceCallDetails(
            InferenceOperationType.Chat,
            model: _deploymentName,
            providerName: "AzureOpenAI");

        List<ChatMessage> snapshot;
        lock (history) { snapshot = [.. history]; }

        ChatResponse chatResponse;
        using (var inferenceScope = InferenceScope.Start(request, inferenceDetails, agentDetails))
        {
            chatResponse = await _chatClient.GetResponseAsync(snapshot, _chatOptions, ct);

            if (chatResponse.Usage is { } usage)
            {
                if (usage.InputTokenCount is { } inputTokens)
                    inferenceScope.RecordInputTokens((int)inputTokens);
                if (usage.OutputTokenCount is { } outputTokens)
                    inferenceScope.RecordOutputTokens((int)outputTokens);
            }

            var finishReason = chatResponse.FinishReason?.Value ?? "stop";
            inferenceScope.RecordFinishReasons([finishReason]);
        }

        lock (history)
        {
            history.AddRange(chatResponse.Messages);
        }

        // === ExecuteToolScope: record each tool invocation ===
        var toolCalls = chatResponse.Messages
            .SelectMany(m => m.Contents.OfType<FunctionCallContent>())
            .GroupBy(fc => fc.CallId ?? fc.Name ?? "")
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var funcResult in chatResponse.Messages
            .SelectMany(m => m.Contents.OfType<FunctionResultContent>()))
        {
            toolCalls.TryGetValue(funcResult.CallId ?? "", out var matchingCall);

            var toolDetails = new ToolCallDetails(
                toolName: matchingCall?.Name ?? "unknown",
                arguments: matchingCall?.Arguments is { } args ? JsonSerializer.Serialize(args) : null,
                toolCallId: funcResult.CallId);

            using var toolScope = ExecuteToolScope.Start(request, toolDetails, agentDetails);
            if (funcResult.Result is not null)
            {
                toolScope.RecordResponse(funcResult.Result.ToString()!);
            }
        }

        // Extract citations from tool results.
        var citations = chatResponse.Messages
            .SelectMany(m => m.Contents.OfType<FunctionResultContent>())
            .Where(frc => frc.Result is not null)
            .SelectMany(frc =>
            {
                try
                {
                    var json = JsonSerializer.Deserialize<JsonElement>(frc.Result!.ToString()!);
                    if (json.TryGetProperty("structuredContent", out var sc) &&
                        sc.TryGetProperty("results", out var results))
                    {
                        return results.EnumerateArray()
                            .Where(r => r.TryGetProperty("contentUrl", out _))
                            .Select(r => (
                                Title: r.GetProperty("title").GetString() ?? "",
                                Url: r.GetProperty("contentUrl").GetString() ?? "",
                                Content: r.TryGetProperty("content", out var c) ? c.GetString() ?? "" : ""
                            ));
                    }
                }
                catch { }
                return [];
            })
            .DistinctBy(c => c.Url)
            .Take(5).ToList();

        var responseText = chatResponse.Text;

        for (int i = 0; i < citations.Count; i++)
        {
            responseText += $"[{i + 1}] ";
        }

        // === OutputScope: record the agent's reply ===
        using (OutputScope.Start(request, new Response([responseText]), agentDetails))
        {
        }

        var responseMsg = TeamsActivity.CreateBuilder()
            .WithText(responseText, TextFormats.Markdown)
            .AddMention(context.Activity?.From!)
            .Build();

        responseMsg.AddAIGenerated();

        for (int i = 0; i < citations.Count; i++)
        {
            var citation = citations[i];
            var abstract_ = citation.Content.Length > 160 ? citation.Content[..157] + "..." : citation.Content;
            responseMsg.AddCitation(i + 1, new CitationAppearance() { Name = citation.Title, Url = new Uri(citation.Url), Abstract = abstract_, Icon = CitationIcon.Text });
        }

        await context.Send(responseMsg, ct);
    }
}
