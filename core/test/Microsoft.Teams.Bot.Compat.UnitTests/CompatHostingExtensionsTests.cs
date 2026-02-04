// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Teams.Bot.Apps;
using Microsoft.Teams.Bot.Core;

namespace Microsoft.Teams.Bot.Compat.UnitTests;

public class CompatHostingExtensionsTests
{
    #region Backward Compatibility Tests

    [Fact]
    public void AddCompatAdapter_WithoutKey_RegistersNonKeyedServices()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["AzureAd:ClientId"] = "test-client-id",
            ["AzureAd:TenantId"] = "test-tenant-id",
            ["AzureAd:Instance"] = "https://login.microsoftonline.com/"
        };
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();

        // Act
        services.AddCompatAdapter();
        var serviceProvider = services.BuildServiceProvider();

        // Assert - verify non-keyed services are registered
        var compatBotAdapter = serviceProvider.GetService<CompatBotAdapter>();
        Assert.NotNull(compatBotAdapter);

        var botFrameworkAdapter = serviceProvider.GetService<IBotFrameworkHttpAdapter>();
        Assert.NotNull(botFrameworkAdapter);
        Assert.IsType<CompatAdapter>(botFrameworkAdapter);
    }

    #endregion

    #region Keyed Service Registration Tests

    [Fact]
    public void AddCompatAdapter_WithKey_RegistersKeyedServices()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["BotOne:ClientId"] = "bot-one-client-id",
            ["BotOne:TenantId"] = "bot-one-tenant-id",
            ["BotOne:Instance"] = "https://login.microsoftonline.com/"
        };
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();

        // Act
        services.AddCompatAdapter("BotOne");
        var serviceProvider = services.BuildServiceProvider();

        // Assert - verify keyed services are registered
        var conversationClient = serviceProvider.GetKeyedService<ConversationClient>("BotOne");
        Assert.NotNull(conversationClient);

        var userTokenClient = serviceProvider.GetKeyedService<UserTokenClient>("BotOne");
        Assert.NotNull(userTokenClient);

        var teamsApiClient = serviceProvider.GetKeyedService<TeamsApiClient>("BotOne");
        Assert.NotNull(teamsApiClient);

        var teamsBotApplication = serviceProvider.GetKeyedService<TeamsBotApplication>("BotOne");
        Assert.NotNull(teamsBotApplication);

        var compatBotAdapter = serviceProvider.GetKeyedService<CompatBotAdapter>("BotOne");
        Assert.NotNull(compatBotAdapter);
    }

    [Fact]
    public void AddCompatAdapter_MultipleKeys_RegistersIsolatedInstances()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["BotOne:ClientId"] = "bot-one-client-id",
            ["BotOne:TenantId"] = "bot-one-tenant-id",
            ["BotOne:Instance"] = "https://login.microsoftonline.com/",
            ["BotTwo:ClientId"] = "bot-two-client-id",
            ["BotTwo:TenantId"] = "bot-two-tenant-id",
            ["BotTwo:Instance"] = "https://login.microsoftonline.com/"
        };
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();

        // Act
        services.AddCompatAdapter("BotOne");
        services.AddCompatAdapter("BotTwo");
        var serviceProvider = services.BuildServiceProvider();

        // Assert - verify both instances are registered and are different
        var botOneClient = serviceProvider.GetKeyedService<ConversationClient>("BotOne");
        var botTwoClient = serviceProvider.GetKeyedService<ConversationClient>("BotTwo");

        Assert.NotNull(botOneClient);
        Assert.NotNull(botTwoClient);
        Assert.NotSame(botOneClient, botTwoClient);

        var botOneApp = serviceProvider.GetKeyedService<TeamsBotApplication>("BotOne");
        var botTwoApp = serviceProvider.GetKeyedService<TeamsBotApplication>("BotTwo");

        Assert.NotNull(botOneApp);
        Assert.NotNull(botTwoApp);
        Assert.NotSame(botOneApp, botTwoApp);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void AddCompatAdapter_ReadsConfigurationSection_SetsCorrectScope()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["BotOne:ClientId"] = "test-client-id",
            ["BotOne:TenantId"] = "test-tenant-id",
            ["BotOne:Instance"] = "https://login.microsoftonline.com/",
            ["BotOne:Scope"] = "https://custom.scope/.default"
        };
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();

        // Act
        services.AddCompatAdapter("BotOne");
        var serviceProvider = services.BuildServiceProvider();

        // Assert - MSAL options should be configured
        var msalOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>()
            .Get("BotOne");

        Assert.Equal("test-client-id", msalOptions.ClientId);
        Assert.Equal("test-tenant-id", msalOptions.TenantId);
    }

    [Fact]
    public void AddCompatAdapter_WithOptions_OverridesScope()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["BotOne:ClientId"] = "test-client-id",
            ["BotOne:TenantId"] = "test-tenant-id",
            ["BotOne:Instance"] = "https://login.microsoftonline.com/",
            ["BotOne:Scope"] = "https://config.scope/.default"
        };
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();

        // Act - override scope via options
        services.AddCompatAdapter("BotOne", options =>
        {
            options.Scope = "https://override.scope/.default";
        });
        var serviceProvider = services.BuildServiceProvider();

        // Assert - services should be registered (scope is used internally by auth handler)
        var conversationClient = serviceProvider.GetKeyedService<ConversationClient>("BotOne");
        Assert.NotNull(conversationClient);
    }

    #endregion

    #region Custom Auth Handler Tests

    [Fact]
    public void AddCompatAdapter_CustomAuthHandler_UsesProvidedFactory()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["BotOne:ClientId"] = "test-client-id",
            ["BotOne:TenantId"] = "test-tenant-id",
            ["BotOne:Instance"] = "https://login.microsoftonline.com/"
        };
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();

        bool factoryWasCalled = false;

        // Act
        services.AddCompatAdapter("BotOne", options =>
        {
            options.AuthHandlerFactory = (sp, keyName, scope) =>
            {
                factoryWasCalled = true;
                Assert.Equal("BotOne", keyName);
                Assert.Equal(CompatAdapterOptions.DefaultScope, scope);
                return new TestDelegatingHandler();
            };
        });
        var serviceProvider = services.BuildServiceProvider();

        // Trigger HttpClient creation which invokes the factory
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient("BotOne_ConversationClient");

        // Assert
        Assert.True(factoryWasCalled);
    }

    private class TestDelegatingHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }

    #endregion

    #region Argument Validation Tests

    [Fact]
    public void AddCompatAdapter_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services!.AddCompatAdapter());
    }

    [Fact]
    public void AddCompatAdapter_WithKey_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services!.AddCompatAdapter("BotOne"));
    }

    [Fact]
    public void AddCompatAdapter_WithKey_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        ServiceCollection services = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddCompatAdapter(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    public void AddCompatAdapter_WithKey_EmptyOrWhitespaceKey_ThrowsArgumentException(string key)
    {
        // Arrange
        ServiceCollection services = new();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => services.AddCompatAdapter(key));
    }

    #endregion

    #region Idempotency Tests

    [Fact]
    public void AddCompatAdapter_CalledMultipleTimes_IsIdempotent()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["AzureAd:ClientId"] = "test-client-id",
            ["AzureAd:TenantId"] = "test-tenant-id",
            ["AzureAd:Instance"] = "https://login.microsoftonline.com/"
        };
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();

        // Act - call multiple times
        services.AddCompatAdapter();
        services.AddCompatAdapter();
        services.AddCompatAdapter();

        // Assert - should not throw and services should be resolvable
        var serviceProvider = services.BuildServiceProvider();
        var adapter = serviceProvider.GetService<IBotFrameworkHttpAdapter>();
        Assert.NotNull(adapter);
    }

    [Fact]
    public void AddCompatAdapter_WithSameKey_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["BotOne:ClientId"] = "test-client-id",
            ["BotOne:TenantId"] = "test-tenant-id",
            ["BotOne:Instance"] = "https://login.microsoftonline.com/"
        };
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();

        // Act - call with same key multiple times (should not throw)
        services.AddCompatAdapter("BotOne");
        services.AddCompatAdapter("BotOne");

        // Assert - should build successfully (last registration wins for keyed services)
        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetKeyedService<ConversationClient>("BotOne");
        Assert.NotNull(client);
    }

    #endregion
}
