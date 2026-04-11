// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.Bot.Core.Hosting;

namespace Microsoft.Teams.Bot.Core.UnitTests.Hosting;

public class BotConfigTests
{
    [Fact]
    public void Resolve_WithIConfigurationRegistered_ResolvesWithoutBuildingContainer()
    {
        // Arrange
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureAd:ClientId"] = "client-abc",
                ["AzureAd:TenantId"] = "tenant-xyz"
            })
            .Build();

        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(config);

        // Act – must not call BuildServiceProvider internally
        BotConfig result = BotConfig.Resolve(services, "AzureAd");

        // Assert
        Assert.Equal("client-abc", result.ClientId);
        Assert.Equal("tenant-xyz", result.TenantId);
    }

    [Fact]
    public void Resolve_WithoutIConfigurationRegistered_ThrowsInvalidOperationException()
    {
        // Arrange – deliberately no IConfiguration in the collection
        ServiceCollection services = new();

        // Act & Assert – must fail fast rather than building a throwaway container
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => BotConfig.Resolve(services, "AzureAd"));

        Assert.Contains("IConfiguration", ex.Message);
    }

    [Fact]
    public void AddBotAuthentication_WithoutIConfiguration_ThrowsInvalidOperationException()
    {
        // Verify that AddBotAuthentication (which calls ResolveBotConfig internally)
        // also fails fast when IConfiguration is absent.
        ServiceCollection services = new();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => services.AddBotAuthentication(aadSectionName: "AzureAd"));

        Assert.Contains("IConfiguration", ex.Message);
    }

    [Fact]
    public void AddBotAuthentication_WithIConfiguration_DoesNotThrow()
    {
        // Verify that the normal path (IConfiguration registered) works end-to-end.
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureAd:ClientId"] = "client-abc",
                ["AzureAd:TenantId"] = "tenant-xyz"
            })
            .Build();

        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(config);

        // Should complete without throwing
        services.AddBotAuthentication(aadSectionName: "AzureAd");
    }
}
