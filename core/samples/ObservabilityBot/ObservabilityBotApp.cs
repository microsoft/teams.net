// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Text.Json;
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

        var (responseText, citations) = await GetChatResponseAsync(history);

        var responseMsg = TeamsActivity.CreateBuilder()
            .WithText(responseText, TextFormats.Markdown)
            .AddMention(context.Activity?.From!)
            .Build();

        responseMsg.AddAIGenerated();

        for (int i = 0; i < citations.Count; i++)
        {
            var citation = citations[i];
            var abstract_ = citation.Content.Length > 400 ? citation.Content[..200] + "..." : citation.Content;
            responseMsg.AddCitation(i + 1, new CitationAppearance() { Name = citation.Title, Url = new Uri(citation.Url), Abstract = abstract_, Icon = CitationIcon.Text });
        }

        await context.Send(responseMsg, ct);
    }

    private async Task<(string ResponseText, List<(string Title, string Url, string Content)> Citations)> GetChatResponseAsync(List<ChatMessage> history)
    {
        List<ChatMessage> snapshot;
        lock (history)
        {
            snapshot = [.. history];
        }

        ChatResponse response = await _chatClient.GetResponseAsync(snapshot, _chatOptions);

        lock (history)
        {
            history.AddRange(response.Messages);
        }

        var citations = response.Messages
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

        var responseText = response.Text;

        for (int i = 1; i < citations.Count; i++)
        {
            responseText += $"[{i}] ";
        }

        return (responseText, citations);
    }
}
