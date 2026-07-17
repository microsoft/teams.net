// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ClientModel;
using System.Collections.Concurrent;
using System.ComponentModel;
using A2ABot.A2A;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using AgentCard = A2A.AgentCard;

namespace A2ABot;

// LLM with a single `handoff_to_peer` tool. The agent framework owns chat
// history via AgentThread; we cache one thread per Teams conversation and
// pre-seed it via A2AServer when a peer hands off a user.
internal sealed class Agent
{
    private static readonly AsyncLocal<TurnIdentity?> CurrentTurn = new();

    private readonly Config _config;
    private readonly A2AClient _a2aClient;
    private readonly ILogger<Agent> _logger;
    private readonly IChatClient _chatClient;
    private readonly ConcurrentDictionary<string, AgentThread> _threads = new();
    private readonly SemaphoreSlim _initLock = new(1, 1);

    private ChatClientAgent? _agent;

    public Agent(Config config, A2AClient a2aClient, IConfiguration configuration, ILogger<Agent> logger)
    {
        _config = config;
        _a2aClient = a2aClient;
        _logger = logger;

        string endpoint = Require(configuration, "AzureOpenAI:Endpoint");
        string apiKey = Require(configuration, "AzureOpenAI:ApiKey");
        string deployment = Require(configuration, "AzureOpenAI:Deployment");

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? endpointUri))
            throw new InvalidOperationException($"AzureOpenAI:Endpoint is not a valid absolute URI: '{endpoint}'.");

        AzureOpenAIClient azure = new(endpointUri, new ApiKeyCredential(apiKey));
        _chatClient = azure.GetChatClient(deployment).AsIChatClient();
    }

    private static string Require(IConfiguration cfg, string key)
    {
        string? value = cfg[key];
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"{key} is required (set it in appsettings.json or environment).");
        return value;
    }

    public async Task<string> RunAsync(
        string convId,
        TurnIdentity identity,
        string userText,
        CancellationToken ct)
    {
        ChatClientAgent agent = await EnsureAgentAsync(ct);
        AgentThread thread = _threads.GetOrAdd(convId, _ => agent.GetNewThread());

        CurrentTurn.Value = identity;

        AgentRunResponse response = await agent.RunAsync(userText, thread, cancellationToken: ct);
        return response.Text ?? string.Empty;
    }

    // Generate the proactive opening message when a peer hands off a user.
    // Runs the LLM with the handoff context as the user turn so the model
    // both greets the user AND answers the question that came in the
    // summary. The resulting turn is left in the thread, so subsequent
    // user replies continue the conversation naturally.
    public async Task<string> GreetWithHandoffAsync(
        string convId, string fromBot, string userName, string summary, CancellationToken ct)
    {
        ChatClientAgent agent = await EnsureAgentAsync(ct);
        AgentThread thread = _threads.GetOrAdd(convId, _ => agent.GetNewThread());

        string contextPrompt =
            $"[handoff context from {fromBot}] The user {userName} was just handed off to you. " +
            $"They asked: \"{summary}\". " +
            $"Greet them warmly, acknowledge that {fromBot} connected you, and answer their question directly.";

        AgentRunResponse response = await agent.RunAsync(contextPrompt, thread, cancellationToken: ct);
        return response.Text ?? string.Empty;
    }

    private async Task<ChatClientAgent> EnsureAgentAsync(CancellationToken ct)
    {
        if (_agent is not null) return _agent;

        await _initLock.WaitAsync(ct);
        try
        {
            if (_agent is not null) return _agent;

            string peerDescription = await TryFetchPeerDescriptionAsync(ct);

            AIFunction handoffTool = AIFunctionFactory.Create(HandoffToPeerAsync, new AIFunctionFactoryOptions
            {
                Name = "handoff_to_peer",
                Description = $"Hand off the current user to {_config.PeerName} when {_config.PeerName}'s expertise is a better fit. Pass a concise summary of the discussion so {_config.PeerName} can pick up cold. {_config.PeerName} will then message the user directly.",
            });

            string instructions = $"""
            You are {_config.Name}, a Teams bot. Your specialty: {_config.Description}

            You have one peer:
            - {_config.PeerName}: {peerDescription}

            Guidelines:
            - If the user's question fits {_config.PeerName}'s specialty better than your own, call handoff_to_peer with a clear summary. Then briefly tell the user you're handing them over.
            - Otherwise, answer directly.
            - If you see a "[handoff context from X]" note, the previous bot has already connected the user with you and described their question — greet the user warmly, briefly mention X sent them, and **answer the question directly** in the same message. Don't just ask "how can I help?" — the question is already in the context.
            - Keep replies short and conversational.
            """;

            _agent = new ChatClientAgent(
                _chatClient,
                instructions: instructions,
                name: _config.Name,
                description: _config.Description,
                tools: [handoffTool]);

            return _agent;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task<string> TryFetchPeerDescriptionAsync(CancellationToken ct)
    {
        try
        {
            AgentCard card = await _a2aClient.GetPeerCardAsync(ct);
            return card.Description ?? "(no description)";
        }
        catch
        {
            return "(peer card not reachable at startup)";
        }
    }

    private async Task<string> HandoffToPeerAsync(
        [Description("Concise summary of what's been discussed and the user's current question, written so the peer can pick up cold.")] string summary,
        CancellationToken ct)
    {
        TurnIdentity? turn = CurrentTurn.Value;
        if (turn is null)
        {
            // Called from a handoff greeting (no identity) — guard against ping-pong.
            return "handoff_to_peer is unavailable in this context.";
        }

        _logger.LogInformation(
            "[{Bot}] handoff_to_peer firing → peer={Peer} user={User} aadId={AadId} tenant={TenantId}",
            _config.Name, _config.PeerName, turn.UserName, turn.AadObjectId, turn.TenantId);

        await _a2aClient.SendHandoffAsync(
            new HandoffMessage("handoff", turn.AadObjectId, turn.UserName, summary, _config.Name, turn.TenantId, turn.ServiceUrl),
            ct);

        _logger.LogInformation("[{Bot}] handoff_to_peer OK", _config.Name);
        return $"Handoff to {_config.PeerName} confirmed. {_config.PeerName} will message the user directly.";
    }
}
