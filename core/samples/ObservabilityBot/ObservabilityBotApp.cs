// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Agents.A365.Observability.Runtime.Tracing.Contracts;
using Microsoft.Agents.A365.Observability.Runtime.Tracing.Scopes;
using Microsoft.Extensions.AI;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Clients;
using Microsoft.Teams.Apps.OAuth;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Apps.Schema.Entities;
using Microsoft.Teams.Apps.State;
using Microsoft.Teams.Core;

namespace ObservabilityBot;

public class ObservabilityBotApp : TeamsBotApplication
{
    private const string OAuthConnectionName = "sso";
    private readonly IChatClient _chatClient;
    private readonly ChatOptions _chatOptions;
    private readonly ApiClient _teamsApiClient;
    private readonly ConcurrentDictionary<string, List<ChatMessage>> _chatHistories = new();
    private readonly string _deploymentName;
    private readonly OAuthFlow _oauthFlow;

    public ObservabilityBotApp(
        ApiClient teamsApiClient,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ObservabilityBotApp> logger,
        IChatClient chatClient,
        ChatOptions chatOptions,
        TeamsBotApplicationOptions? teamsOptions = null,
        TurnStateLoader? turnStateLoader = null)
        : base(teamsApiClient, httpContextAccessor, logger, teamsOptions, turnStateLoader)
    {
        _teamsApiClient = teamsApiClient;
        _chatClient = chatClient;
        _chatOptions = chatOptions;
        _deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT") ?? "unknown";
        _oauthFlow = this.GetOAuthFlow(OAuthConnectionName);

        _oauthFlow.OnSignInComplete(async (context, tokenResponse, ct) =>
        {
            await context.SendAsync($"Signed in to `{tokenResponse.ConnectionName}`.", ct);
        });

        _oauthFlow.OnSignInFailure(async (context, failure, ct) =>
        {
            string details = string.IsNullOrWhiteSpace(failure?.Message) ? "Sign-in failed." : $"Sign-in failed: {failure.Message}";
            await context.SendAsync(details, ct);
        });

        this.OnMessage(HandleMessageAsync);
    }

    private async Task HandleMessageAsync(Context<MessageActivity> context, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(context.Activity);
        ArgumentNullException.ThrowIfNull(context.Activity.Conversation);
        ArgumentNullException.ThrowIfNull(context.Activity.Conversation.Id);

        if (await TryHandleCommandAsync(context, ct).ConfigureAwait(false))
        {
            return;
        }

        await context.TypingAsync(ct);

        string conversationId = context.Activity.Conversation.Id;
        List<ChatMessage> history = context.State.UserState?.Get<List<ChatMessage>>() ?? [];

        lock (history)
        {
            history.Add(new ChatMessage(ChatRole.User, context.Activity.Text));
        }

        // Build Agent365 scope contracts from the turn context.
        TeamsChannelAccount? recipient = context.Activity.Recipient;
        var agentDetails = new AgentDetails(
            agentId: recipient?.AgenticAppInstanceId ?? recipient?.Id,
            agentName: recipient?.Name,
            agenticUserId: recipient?.AgenticUserId,
            agentBlueprintId: recipient?.AgenticBlueprintId,
            tenantId: recipient?.TenantId);

        var request = new Request(
            content: context.Activity.Text,
            conversationId: conversationId,
            channel: new Channel(context.Activity.ChannelId));

        // === InvokeAgentScope: wraps the entire agent turn ===
        // Opened here (not by the SDK) so we can reliably record both input and output.
        // The SDK has already set cert-required baggage on Baggage.Current via TeamsBaggageBuilder,
        // so this span inherits tenant.id, agent.id, user.id etc. automatically.
        var invokeAgentScopeDetails = new InvokeAgentScopeDetails(context.Activity.ServiceUrl);
        using var invokeScope = InvokeAgentScope.Start(request, invokeAgentScopeDetails, agentDetails);

        try
        {
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

                string finishReason = chatResponse.FinishReason?.Value ?? "stop";
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

            foreach (FunctionResultContent? funcResult in chatResponse.Messages
                .SelectMany(m => m.Contents.OfType<FunctionResultContent>()))
            {
                toolCalls.TryGetValue(funcResult.CallId ?? "", out FunctionCallContent? matchingCall);

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

            string responseText = chatResponse.Text;

            // Record output on the top-level invoke_agent span before it closes.
            invokeScope.RecordOutputMessages([responseText]);

            MessageActivityInputBuilder builder = MessageActivityInput.CreateBuilder()
                .WithText(responseText, TextFormats.Markdown)
                .AddMention(context.Activity?.From!)
                .AddAIGenerated();

            await context.SendAsync(builder.Build(), ct);
        }
        catch (Exception ex)
        {
            invokeScope.RecordError(ex);
            throw;
        }
        finally
        {
            context.State.UserState?.Set(history);
        }
    }

    private async Task<bool> TryHandleCommandAsync(Context<MessageActivity> context, CancellationToken ct)
    {
        string text = context.Activity.TextWithoutMentions ?? string.Empty;
        string command = text.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(command))
        {
            return false;
        }

        if (command == "help")
        {
            await context.SendAsync(
                MessageActivityInput.CreateBuilder()
                    .WithText(
                        """
                        **ObservabilityBot commands**
                        - `login` - start OAuth sign-in flow
                        - `logout` - sign out from OAuth connection
                        - `status` - show OAuth connection status
                        - `team` - call TeamClient and show current team details
                        - anything else - AI response path
                        """,
                        TextFormats.Markdown)
                    .Build(),
                ct).ConfigureAwait(false);
            return true;
        }

        if (command == "login")
        {
            string? token = await _oauthFlow.SignInAsync(context, ct).ConfigureAwait(false);
            if (token is not null)
            {
                await context.SendAsync("Already signed in.", ct).ConfigureAwait(false);
            }
            return true;
        }

        if (command == "logout")
        {
            await _oauthFlow.SignOutAsync(context, ct).ConfigureAwait(false);
            await context.SendAsync("Signed out.", ct).ConfigureAwait(false);
            return true;
        }

        if (command == "status")
        {
            IList<GetTokenStatusResult> statuses = await _oauthFlow.GetConnectionStatusAsync(context, ct).ConfigureAwait(false);
            string statusText = string.Join(
                "\n",
                statuses.Select(s => $"- `{s.ConnectionName}`: {(s.HasToken == true ? "connected" : "not connected")}"));

            await context.SendAsync(
                MessageActivityInput.CreateBuilder()
                    .WithText($"**OAuth status**\n{statusText}", TextFormats.Markdown)
                    .Build(),
                ct).ConfigureAwait(false);
            return true;
        }

        if (command == "team")
        {
            string? teamId = context.Activity.ChannelData?.TeamsTeamId ?? context.Activity.ChannelData?.Team?.Id;
            if (string.IsNullOrWhiteSpace(teamId))
            {
                await context.SendAsync("No team id found on this activity. Try in a Team channel.", ct).ConfigureAwait(false);
                return true;
            }

            ApiClient client = _teamsApiClient.ForActivity(context.Activity);
            Team? team = await client.Teams.GetByIdAsync(teamId, ct).ConfigureAwait(false);
            List<TeamsChannel>? channels = await client.Teams.GetConversationsAsync(teamId, ct).ConfigureAwait(false);

            string response = $$"""
                **TeamClient result**
                - team id: `{{teamId}}`
                - team name: {{team?.Name ?? "(unknown)"}}
                - channels: {{channels?.Count ?? 0}}
                """;

            await context.SendAsync(MessageActivityInput.CreateBuilder().WithText(response, TextFormats.Markdown).Build(), ct).ConfigureAwait(false);
            return true;
        }

        return false;
    }
}
