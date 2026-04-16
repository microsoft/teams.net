// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Hosting;
using Xunit.Abstractions;

namespace Microsoft.Bot.Core.Tests;

public class UserTokenClientTests
{
    private readonly ServiceProvider _serviceProvider;
    private readonly UserTokenClient _userTokenClient;

    public UserTokenClientTests(ITestOutputHelper outputHelper)
    {
        IConfigurationBuilder builder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddEnvironmentVariables();

        IConfiguration configuration = builder.Build();

        ServiceCollection services = new();
        services.AddLogging((builder) => {
            builder.AddXUnit(outputHelper);
            builder.AddFilter("System.Net", LogLevel.Warning);
            builder.AddFilter("Microsoft.Identity", LogLevel.Error);
            builder.AddFilter("Microsoft.Teams", LogLevel.Trace);
        });
        services.AddSingleton(configuration);
        services.AddBotApplication<BotApplication>();
        _serviceProvider = services.BuildServiceProvider();
        _userTokenClient = _serviceProvider.GetRequiredService<UserTokenClient>();
    }

    #region Integration Tests

    [Trait("Category", "needs-oauth-connection")]
    [Fact]
    public async Task GetTokenAsync_WithValidParams()
    {
        string userId = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? throw new InvalidOperationException("TEST_USER_ID environment variable not set");
        string connectionName = Environment.GetEnvironmentVariable("TEST_CONNECTION_NAME") ?? throw new InvalidOperationException("TEST_CONNECTION_NAME environment variable not set");

        var result = await _userTokenClient.GetTokenAsync(
            userId,
            connectionName,
            "msteams",
            cancellationToken: CancellationToken.None);

        // GetTokenAsync returns null when no token is found (returnNullOnNotFound: true)
        Console.WriteLine($"GetTokenAsync result: {(result != null ? "Token found" : "No token")}");
    }

    [Fact]
    public async Task GetTokenStatusAsync_WithValidParams()
    {
        string userId = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? throw new InvalidOperationException("TEST_USER_ID environment variable not set");

        var result = await _userTokenClient.GetTokenStatusAsync(
            userId,
            "msteams",
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotEmpty(result);

        Console.WriteLine($"Token status results: {result.Length}");
        foreach (var status in result)
        {
            Console.WriteLine($"  - ConnectionName: {status.ConnectionName}, HasToken: {status.HasToken}");
        }
    }

    [Trait("Category", "needs-oauth-connection")]
    [Fact]
    public async Task GetSignInResource_WithValidParams()
    {
        string userId = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? throw new InvalidOperationException("TEST_USER_ID environment variable not set");
        string connectionName = Environment.GetEnvironmentVariable("TEST_CONNECTION_NAME") ?? throw new InvalidOperationException("TEST_CONNECTION_NAME environment variable not set");

        var result = await _userTokenClient.GetSignInResource(
            userId,
            connectionName,
            "msteams",
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.SignInLink);

        Console.WriteLine($"Sign-in resource:");
        Console.WriteLine($"  - SignInLink: {result.SignInLink}");
    }

    [Trait("Category", "needs-oauth-connection")]
    [Fact]
    public async Task ExchangeTokenAsync_WithValidParams()
    {
        string userId = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? throw new InvalidOperationException("TEST_USER_ID environment variable not set");
        string connectionName = Environment.GetEnvironmentVariable("TEST_CONNECTION_NAME") ?? throw new InvalidOperationException("TEST_CONNECTION_NAME environment variable not set");

        var result = await _userTokenClient.ExchangeTokenAsync(
            userId,
            connectionName,
            "msteams",
            "test-exchange-token",
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);

        Console.WriteLine($"Exchange token result: Token={result.Token != null}");
    }

    [Fact]
    public async Task SignOutUserAsync_WithValidParams()
    {
        string userId = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? throw new InvalidOperationException("TEST_USER_ID environment variable not set");
        string connectionName = Environment.GetEnvironmentVariable("TEST_CONNECTION_NAME") ?? throw new InvalidOperationException("TEST_CONNECTION_NAME environment variable not set");

        await _userTokenClient.SignOutUserAsync(
            userId,
            connectionName,
            "msteams",
            cancellationToken: CancellationToken.None);

        // If no exception was thrown, the sign-out was successful
        Console.WriteLine("SignOutUserAsync completed successfully");
    }

    [Trait("Category", "needs-oauth-connection")]
    [Fact]
    public async Task GetAadTokensAsync_WithValidParams()
    {
        string userId = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? throw new InvalidOperationException("TEST_USER_ID environment variable not set");
        string connectionName = Environment.GetEnvironmentVariable("TEST_CONNECTION_NAME") ?? throw new InvalidOperationException("TEST_CONNECTION_NAME environment variable not set");

        var result = await _userTokenClient.GetAadTokensAsync(
            userId,
            connectionName,
            "msteams",
            ["https://graph.microsoft.com"],
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);

        Console.WriteLine($"AAD tokens result: {result.Count} entries");
        foreach (var entry in result)
        {
            Console.WriteLine($"  - Resource: {entry.Key}, HasToken: {entry.Value?.Token != null}");
        }
    }

    #endregion
}
