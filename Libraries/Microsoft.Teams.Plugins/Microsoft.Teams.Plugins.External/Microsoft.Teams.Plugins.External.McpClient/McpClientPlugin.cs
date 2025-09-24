using Json.Schema;

using Microsoft.Teams.AI;
using Microsoft.Teams.AI.Prompts;
using Microsoft.Teams.Common.Logging;

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Microsoft.Teams.Plugins.External.McpClient;

public class McpClientPlugin : BaseChatPlugin
{
    public readonly string Version;

    public readonly string Name;

    public readonly int RefetchTimeoutMs;

    public readonly IDictionary<string, McpCachedValue> Cache;

    private readonly ILogger _logger;
    private readonly IDictionary<string, McpClientPluginParams> _mcpServerParams;

    public McpClientPlugin(McpClientPluginOptions? options = null)
    {
        options ??= new McpClientPluginOptions();
        Name = options.Name;
        Version = options.Version;
        RefetchTimeoutMs = options.RefetchTimeoutMs;
        Cache = options.Cache ?? new Dictionary<string, McpCachedValue>();

        _logger = options.Logger?.Child(Name) ?? new ConsoleLogger();
        _mcpServerParams = new Dictionary<string, McpClientPluginParams>();

        if (options.Cache != null)
        {
            foreach (var entry in options.Cache)
            {
                if (entry.Value.AvailableTools is not null && entry.Value.LastFetched is null)
                {
                    entry.Value.LastFetched = DateTimeOffset.UtcNow;
                }
            }
        }
    }

    /// <summary>
    /// Add or update an MCP server to be used by the plugin.
    /// </summary>
    /// <param name="url">MCP server URL to connect to</param>
    /// <param name="pluginParams">Optional configuration parameters for the server</param>
    public McpClientPlugin UseMcpServer(string url, McpClientPluginParams? pluginParams = null)
    {
        _mcpServerParams[url] = pluginParams ?? new McpClientPluginParams();

        // Update cache if tools are provided
        if (pluginParams?.AvailableTools is not null)
        {
            Cache[url] = new McpCachedValue()
            {
                AvailableTools = pluginParams.AvailableTools,
                LastFetched = DateTimeOffset.UtcNow,
                Transport = pluginParams.Transport
            };
        }

        return this;
    }

    public override async Task<FunctionCollection> OnBuildFunctions<TOptions>(IChatPrompt<TOptions> prompt, FunctionCollection functions, CancellationToken cancellationToken = default)
    {
        await FetchToolsIfNeeded();

        foreach (var entry in _mcpServerParams)
        {
            string url = entry.Key;
            McpClientPluginParams pluginParams = entry.Value;
            if (Cache.TryGetValue(url, out McpCachedValue? value))
            {
                if (value?.AvailableTools == null)
                {
                    continue;
                }

                foreach (var tool in value.AvailableTools)
                {
                    var function = CreateFunctionFromTool(new Uri(url), tool, pluginParams);
                    functions.Add(function);
                    _logger.Debug($"Added function {function.Name} from MCP server at {url}");
                }
            }
        }

        return functions;
    }

    /// <summary>
    /// Fetch tools from MCP servers if needed.
    /// 
    /// Checks if cached values have expired or if tools have never been fetched. Performs parallel fetching for efficiency.
    /// </summary>
    internal async Task FetchToolsIfNeeded()
    {
        var fetchNeeded = new List<KeyValuePair<string, McpClientPluginParams>>();

        foreach (var entry in _mcpServerParams)
        {
            string url = entry.Key;
            McpClientPluginParams pluginParams = entry.Value;

            // Skip if tools are explicitly provided
            if (pluginParams.AvailableTools is not null)
            {
                continue;
            }

            McpCachedValue? cachedData = Cache.ContainsKey(url) ? Cache[url] : null;
            bool shouldFetch = (cachedData?.AvailableTools is null) || (cachedData?.LastFetched is null) ||
                (DateTimeOffset.UtcNow - cachedData.LastFetched.Value).Milliseconds > (pluginParams.RefetchTimeoutMs ?? RefetchTimeoutMs);

            if (shouldFetch)
            {
                fetchNeeded.Add(new KeyValuePair<string, McpClientPluginParams>(url, pluginParams));
            }
        }

        if (fetchNeeded.Count > 0)
        {
            IList<Task<List<McpToolDetails>>> tasks = [];
            foreach (var entry in fetchNeeded)
            {
                string url = entry.Key;
                McpClientPluginParams pluginParams = entry.Value;
                tasks.Add(FetchToolsFromServer(new Uri(url), pluginParams));
            }
            try
            {
                await Task.WhenAll(tasks);
            }
            catch
            {
                // Suppress all exceptions, but tasks are still awaited
                // Individual task exceptions will be handled below
            }

            var results = fetchNeeded.Zip(tasks);
            foreach (var result in results)
            {
                string url = result.First.Key;
                McpClientPluginParams pluginParams = result.First.Value;
                var fetchTask = result.Second;

                if (!fetchTask.IsCompletedSuccessfully)
                {
                    if (pluginParams.SkipIfUnavailable)
                    {
                        _logger.Error($"Failed to fetch tools from MCP server at {url}, but continuing as SkipIfUnavailable is set.", fetchTask.Exception);
                        continue;
                    }
                    else
                    {
                        throw new Exception($"Failed to fetch tools from MCP server at {url}", fetchTask.Exception);
                    }
                }

                var tools = fetchTask.Result;
                if (!Cache.ContainsKey(url))
                {
                    Cache[url] = new McpCachedValue();
                }

                Cache[url].AvailableTools = tools;
                Cache[url].LastFetched = DateTimeOffset.UtcNow;
                Cache[url].Transport = pluginParams.Transport;

                _logger.Debug($"Cached {tools.Count} tools from MCP server at {url}");
            }
        }
    }

    internal async Task<List<McpToolDetails>> FetchToolsFromServer(Uri url, McpClientPluginParams pluginParams)
    {
        IClientTransport transport = CreateTransport(url, pluginParams.Transport, pluginParams.HeadersFactory());
        var client = await McpClientFactory.CreateAsync(transport);
        var tools = await client.ListToolsAsync();

        // Convert MCP tools to our format
        var mappedTools = tools.Select(t => new McpToolDetails()
        {
            Name = t.Name,
            Description = t.Description,
            InputSchema = t.JsonSchema
        }).ToList();

        return mappedTools;
    }

    internal IClientTransport CreateTransport(Uri url, McpClientTransport transport, IDictionary<string, string>? headers)
    {
        var options = new SseClientTransportOptions() { Endpoint = url };
        switch (transport)
        {
            case McpClientTransport.StreamableHttp:
                options.TransportMode = HttpTransportMode.StreamableHttp;
                break;
            case McpClientTransport.Sse:
                options.TransportMode = HttpTransportMode.Sse;
                break;
            default:
                options.TransportMode = HttpTransportMode.AutoDetect;
                break;
        }
        options.AdditionalHeaders = headers;
        return new SseClientTransport(options);
    }

    internal AI.Function CreateFunctionFromTool(Uri url, McpToolDetails tool, McpClientPluginParams pluginParams)
    {
        return new AI.Function(
            tool.Name,
            tool.Description,
            JsonSchema.FromText(tool.InputSchema?.GetRawText() ?? "{}"),
            async (IDictionary<string, object?> args) =>
            {
                try
                {
                    _logger.Debug($"Making call to {url} for tool {tool.Name}");
                    string result = await CallMcpTool(url, tool, args.AsReadOnly(), pluginParams);
                    _logger.Debug($"Received result from {tool.Name}: {result}");
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error calling MCP tool {tool.Name} at {url}", ex);
                    throw;
                }
            }
        );
    }

    internal async Task<string> CallMcpTool(Uri url, McpToolDetails tool, IReadOnlyDictionary<string, object?> args, McpClientPluginParams pluginParams)
    {
        IClientTransport transport = CreateTransport(url, pluginParams.Transport, pluginParams.HeadersFactory());
        var client = await McpClientFactory.CreateAsync(transport);
        var response = await client.CallToolAsync(tool.Name, args);

        if (response.IsError == true)
        {
            _logger.Warn($"MCP tool call to {tool.Name} return error status");
        }

        return response.Content.Select(c => c.Type == "text" ? ((TextContentBlock)c).Text : "").Aggregate((a, b) => $"{a},{b}");
    }
}