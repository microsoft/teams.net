// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

namespace ExtAIBot;

// Owns the McpClient lifetime, lists tools at startup, and returns them wrapped
// with citation extraction so search results populate the CitationCollector.
sealed class McpToolSet : IAsyncDisposable
{
    private readonly McpClient _client;
    private readonly IList<McpClientTool> _tools;

    private McpToolSet(McpClient client, IList<McpClientTool> tools)
    {
        _client = client;
        _tools = tools;
    }

    public static async Task<McpToolSet> CreateAsync(CancellationToken cancellationToken = default)
    {
        McpClient client = await McpClient.CreateAsync(
            new HttpClientTransport(new HttpClientTransportOptions
            {
                Endpoint = new Uri("https://learn.microsoft.com/api/mcp"),
                Name = "MSLearn",
                TransportMode = HttpTransportMode.StreamableHttp
            }),
            cancellationToken: cancellationToken);

        IList<McpClientTool> tools =
            await client.ListToolsAsync(cancellationToken: cancellationToken);

        return new McpToolSet(client, tools);
    }

    // Returns each MCP tool wrapped so its results feed into the CitationCollector.
    public IList<AITool> GetTools(CitationCollector citations) =>
        [.. _tools.Select(t => new CitationCapturingTool(t, citations))];

    public ValueTask DisposeAsync() => _client.DisposeAsync();
}

// Wraps an McpClientTool, delegating all metadata to it while intercepting
// InvokeCoreAsync to extract citation data from the raw result string.
file sealed class CitationCapturingTool(McpClientTool inner, CitationCollector citations)
    : DelegatingAIFunction(inner)
{
    protected override async ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments arguments,
        CancellationToken cancellationToken)
    {
        object? result = await inner.InvokeAsync(arguments, cancellationToken);
        if (result?.ToString() is string text)
            citations.TryExtract(text);
        return result;
    }
}
