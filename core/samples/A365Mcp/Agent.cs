// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using Microsoft.Extensions.AI;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Teams.Core.Schema;
using ModelContextProtocol.Client;

namespace A365Mcp;

internal class Agent
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<Agent> _logger;
    private readonly ConcurrentDictionary<string, List<ChatMessage>> _histories = new();
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    private readonly IAuthorizationHeaderProvider _authorizationHeaderProvider;

    public Agent(IChatClient chatClient, IHttpClientFactory httpClientFactory, IAuthorizationHeaderProvider authorizationHeaderProvider, ILogger<Agent> logger)
    {
        _chatClient = chatClient;
        _httpClientFactory = httpClientFactory;
        _authorizationHeaderProvider = authorizationHeaderProvider;
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

        string[] scopes = ["ea9ffc3e-8a23-4a7d-836d-234d7c7565c1/.default"];
        var authOptions = new AuthorizationHeaderProviderOptions()
        {
            AcquireTokenOptions = new()
            {
                AuthenticationOptionsName = "AzureAd"
            }
        }.WithAgentUserIdentity(agentic.AgenticAppId, new Guid(agentic.AgenticUserId));
        var authHeader = await _authorizationHeaderProvider.CreateAuthorizationHeaderAsync(scopes, authOptions, cancellationToken: cancellationToken);

        var httpClient = _httpClientFactory.CreateClient("mcp");
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authHeader.Substring("Bearer".Length));

        var teamsMcpServerUrl = $"https://agent365.svc.cloud.microsoft/agents/servers/mcp_TeamsServer";
        var teamsMcpClient = await McpClient.CreateAsync(
            new HttpClientTransport(new()
            {
                Endpoint = new Uri(teamsMcpServerUrl),
                Name = "Agent365 Teams Client"
            }, httpClient));

        var teamsMcpTools = await teamsMcpClient.ListToolsAsync();


        List<ChatMessage> history = _histories.GetOrAdd(conversationId,_ => [new ChatMessage(ChatRole.System, SystemPrompt)]);

        // Serialize turns within a single conversation so concurrent submits
        // (e.g. clarification race) don't interleave history mutations.
        SemaphoreSlim gate = _locks.GetOrAdd(conversationId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            history.Add(new ChatMessage(ChatRole.User, userText));
                        
            ChatOptions options = new()
            {
                Tools =[.. teamsMcpTools]
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
