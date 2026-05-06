// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Teams.Core.Hosting;

namespace Microsoft.Teams.Core.UnitTests.Hosting;

public class AddBotApplicationExtensionsTests
{
    private static ServiceProvider BuildServiceProvider(Dictionary<string, string?> configData, string? aadConfigSectionName = null)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();

        if (aadConfigSectionName is null)
        {
            services.AddConversationClient();
        }
        else
        {
            services.AddConversationClient(aadConfigSectionName);
        }

        return services.BuildServiceProvider();
    }

    private static void AssertMsalOptions(ServiceProvider serviceProvider, string sectionName, string expectedClientId, string expectedTenantId, string expectedInstance = "https://login.microsoftonline.com/")
    {
        MicrosoftIdentityApplicationOptions msalOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>()
            .Get(sectionName);
        Assert.Equal(expectedClientId, msalOptions.ClientId);
        Assert.Equal(expectedTenantId, msalOptions.TenantId);
        Assert.Equal(expectedInstance, msalOptions.Instance);
    }

    [Fact]
    public void AddConversationClient_WithDefaultSection_ConfiguresFromSection()
    {
        // AzureAd is the default Section Name
        // Arrange
        Dictionary<string, string?> configData = new()
        {
            ["AzureAd:ClientId"] = "azuread-client-id",
            ["AzureAd:TenantId"] = "azuread-tenant-id",
            ["AzureAd:Instance"] = "https://login.microsoftonline.com/"
        };

        // Act
        ServiceProvider serviceProvider = BuildServiceProvider(configData);

        // Assert
        AssertMsalOptions(serviceProvider, "AzureAd", "azuread-client-id", "azuread-tenant-id");
    }

    [Fact]
    public void AddConversationClient_WithCustomSectionName_ConfiguresFromCustomSection()
    {
        // Arrange
        Dictionary<string, string?> configData = new()
        {
            ["CustomAuth:ClientId"] = "custom-client-id",
            ["CustomAuth:TenantId"] = "custom-tenant-id",
            ["CustomAuth:Instance"] = "https://login.microsoftonline.com/"
        };

        // Act
        ServiceProvider serviceProvider = BuildServiceProvider(configData, "CustomAuth");

        // Assert
        AssertMsalOptions(serviceProvider, "CustomAuth", "custom-client-id", "custom-tenant-id");
    }

    // --- BotApplicationOptions (AppId) tests ---

    private static ServiceProvider BuildServiceProviderForBotApp(Dictionary<string, string?> configData, string? sectionName = null)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();

        if (sectionName is null)
            services.AddBotApplication();
        else
            services.AddBotApplication(sectionName);

        return services.BuildServiceProvider();
    }

    private static string GetAppId(ServiceProvider serviceProvider) =>
        serviceProvider.GetRequiredService<BotApplicationOptions>().AppId;

    [Fact]
    public void AddBotApplication_WithAzureAdSection_SetsAppIdFromSection()
    {
        // Arrange
        Dictionary<string, string?> configData = new()
        {
            ["AzureAd:ClientId"] = "azuread-client-id",
            ["AzureAd:TenantId"] = "azuread-tenant-id"
        };

        // Act
        ServiceProvider serviceProvider = BuildServiceProviderForBotApp(configData);

        // Assert
        Assert.Equal("azuread-client-id", GetAppId(serviceProvider));
    }

    [Fact]
    public void AddBotApplication_WithCustomSection_SetsAppIdFromCustomSection()
    {
        // Arrange
        Dictionary<string, string?> configData = new()
        {
            ["CustomAuth:ClientId"] = "custom-client-id",
            ["CustomAuth:TenantId"] = "custom-tenant-id"
        };

        // Act
        ServiceProvider serviceProvider = BuildServiceProviderForBotApp(configData, "CustomAuth");

        // Assert
        Assert.Equal("custom-client-id", GetAppId(serviceProvider));
    }
}
