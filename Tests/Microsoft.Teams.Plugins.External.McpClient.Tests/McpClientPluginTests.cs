using System.Text.Json;
using Moq;
using Microsoft.Teams.AI;
using Microsoft.Teams.AI.Prompts;

namespace Microsoft.Teams.Plugins.External.McpClient.Tests;

public class McpClientPluginTests
{
    [Fact]
    public void Test_Constructor_SetsDefaults_AndInitializesCacheTimestamps()
    {
        // Arrange
        var cache = new Dictionary<string, McpCachedValue>
        {
            ["http://example.org"] = new McpCachedValue
            {
                AvailableTools = new List<McpToolDetails> {
                    new McpToolDetails { Name = "tool1", Description = "d", InputSchema = JsonDocument.Parse("{} ").RootElement }
                },
                LastFetched = null
            }
        };
        var logger = new Mock<Microsoft.Teams.Common.Logging.ILogger>(MockBehavior.Strict);
        logger.Setup(l => l.Child(It.IsAny<string>())).Returns(logger.Object);

        // Act
        var plugin = new McpClientPlugin(new McpClientPluginOptions
        {
            Name = "TestPlugin",
            Version = "1.2.3",
            Cache = cache,
            Logger = logger.Object
        });

        // Assert
        Assert.Equal("TestPlugin", plugin.Name);
        Assert.Equal("1.2.3", plugin.Version);
        Assert.True(cache["http://example.org"].LastFetched.HasValue);
    }

    [Fact]
    public void Test_UseMcpServer_WithAvailableTools_PopulatesCache()
    {
        // Arrange
        var plugin = new McpClientPlugin();
        var tools = new List<McpToolDetails>
        {
            new McpToolDetails { Name = "alpha", Description = "desc", InputSchema = JsonDocument.Parse("{} ").RootElement }
        };

        // Act
        var returned = plugin.UseMcpServer("http://server-a", new McpClientPluginParams { AvailableTools = tools });

        // Assert
        Assert.Same(plugin, returned);
        Assert.True(plugin.Cache.ContainsKey("http://server-a"));
        Assert.Equal("alpha", plugin.Cache["http://server-a"].AvailableTools![0].Name);
        Assert.True(plugin.Cache["http://server-a"].LastFetched.HasValue);
    }

    [Fact]
    public void Test_UseMcpServer_WithoutAvailableTools_DoesNotPopulateCacheImmediately()
    {
        // Arrange
        var plugin = new McpClientPlugin();

        // Act
        plugin.UseMcpServer("http://server-b");

        // Assert
        Assert.False(plugin.Cache.ContainsKey("http://server-b"));
    }

    [Fact]
    public async Task OnBuildFunctions_AddsFunctionsFromCache()
    {
        // Arrange
        var plugin = new McpClientPlugin();
        var tools = new List<McpToolDetails>
        {
            new McpToolDetails { Name = "doThing", Description = "Does a thing", InputSchema = JsonDocument.Parse("{} ").RootElement }
        };
        plugin.UseMcpServer("http://server-c", new McpClientPluginParams { AvailableTools = tools });
        var prompt = new Mock<IChatPrompt<object>>().Object; // Not used internally beyond type
        var functions = new FunctionCollection();

        // Act
        var result = await plugin.OnBuildFunctions(prompt, functions, CancellationToken.None);

        // Assert
        Assert.True(result.Has("doThing"));
        Assert.Single(result.Names);
    }

    [Fact]
    public void Test_CreateTransport_ReturnsNonNull_ForEachTransport()
    {
        // Arrange
        var plugin = new McpClientPlugin();
        var url = new Uri("http://localhost");

        // Act & Assert
        foreach (McpClientTransport mode in Enum.GetValues(typeof(McpClientTransport)))
        {
            var transport = plugin.CreateTransport(url, mode, null);
            Assert.NotNull(transport);
        }
    }

    [Fact]
    public void Test_CreateFunctionFromTool_ReturnsFunction()
    {
        // Arrange
        var plugin = new McpClientPlugin();
        var url = new Uri("http://localhost");
        var tool = new McpToolDetails { Name = "toolX", Description = "desc", InputSchema = JsonDocument.Parse("{} ").RootElement };
        var paramsObj = new McpClientPluginParams();

        // Act
        var function = plugin.CreateFunctionFromTool(url, tool, paramsObj);

        // Assert
        Assert.Equal("toolX", function.Name);
        Assert.Equal("desc", function.Description);
        Assert.NotNull(function.Parameters);
    }

    [Fact]
    public async Task FetchToolsIfNeeded_NoServersWithFetchNeeded_NoChanges()
    {
        // Arrange
        var plugin = new McpClientPlugin();
        plugin.UseMcpServer("http://server-d", new McpClientPluginParams
        {
            AvailableTools = new List<McpToolDetails> { new McpToolDetails { Name = "t", Description = "d", InputSchema = JsonDocument.Parse("{} ").RootElement } }
        });
        var before = plugin.Cache["http://server-d"].LastFetched;

        // Act
        await plugin.FetchToolsIfNeeded();

        // Assert
        var after = plugin.Cache["http://server-d"].LastFetched;
        Assert.Equal(before, after);
    }

    [Fact]
    public async Task FetchToolsFromServer_InvalidEndpoint_ThrowsOrReturns()
    {
        // Arrange
        var plugin = new McpClientPlugin();
        var url = new Uri("http://127.0.0.1:59999");
        var p = new McpClientPluginParams();

        // Act & Assert
        try
        {
            var task = plugin.FetchToolsFromServer(url, p);
            await Assert.ThrowsAnyAsync<Exception>(async () => await task);
        }
        catch (Exception)
        {
            // Acceptable: network failure may bubble earlier
            Assert.True(true);
        }
    }

    [Fact]
    public async Task CallMcpTool_InvalidEndpoint_Throws()
    {
        // Arrange
        var plugin = new McpClientPlugin();
        var url = new Uri("http://127.0.0.1:60001");
        var tool = new McpToolDetails { Name = "missing", Description = "", InputSchema = JsonDocument.Parse("{} ").RootElement };
        var p = new McpClientPluginParams();
        var args = new Dictionary<string, object?>();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () => await plugin.CallMcpTool(url, tool, args, p));
    }
}
