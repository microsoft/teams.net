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

    // --- ManagedIdentityOptions (UMI) tests ---

    private static ServiceProvider BuildServiceProviderWithManagedIdentity(Dictionary<string, string?> configData, string? sectionName = null)
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

    [Fact]
    public void AddBotApplication_WithNoClientCredentials_ConfiguresManagedIdentityOptions()
    {
        // Arrange: Configuration with ClientId/TenantId but NO ClientCredentials (implies UMI)
        Dictionary<string, string?> configData = new()
        {
            ["AzureAd:ClientId"] = "umi-client-id",
            ["AzureAd:TenantId"] = "tenant-id"
            // No AzureAd:ClientCredentials
        };

        // Act
        ServiceProvider serviceProvider = BuildServiceProviderWithManagedIdentity(configData);

        // Assert: named options entry under the section name carries the UMI client id
        ManagedIdentityOptions miOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<ManagedIdentityOptions>>()
            .Get("AzureAd");

        Assert.NotNull(miOptions);
        Assert.Equal("umi-client-id", miOptions.UserAssignedClientId);
    }

    [Fact]
    public void AddBotApplication_WithClientCredentials_DoesNotConfigureUmiAsUserAssigned()
    {
        // Arrange: Configuration WITH ClientCredentials (app-only authentication, not UMI)
        Dictionary<string, string?> configData = new()
        {
            ["AzureAd:ClientId"] = "app-client-id",
            ["AzureAd:TenantId"] = "tenant-id",
            ["AzureAd:ClientCredentials:0:SourceType"] = "ClientSecret",
            ["AzureAd:ClientCredentials:0:ClientSecret"] = "secret-value"
        };

        // Act
        ServiceProvider serviceProvider = BuildServiceProviderWithManagedIdentity(configData);

        // Assert: when ClientCredentials are present, the named entry must remain empty
        ManagedIdentityOptions miOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<ManagedIdentityOptions>>()
            .Get("AzureAd");

        Assert.Null(miOptions.UserAssignedClientId);
    }

    [Fact]
    public void AddBotApplication_WithCustomSectionNoClientCredentials_ConfiguresManagedIdentityFromCustomSection()
    {
        // Arrange: Custom section with no ClientCredentials
        Dictionary<string, string?> configData = new()
        {
            ["CustomAuth:ClientId"] = "custom-umi-client-id",
            ["CustomAuth:TenantId"] = "custom-tenant-id"
        };

        // Act
        ServiceProvider serviceProvider = BuildServiceProviderWithManagedIdentity(configData, "CustomAuth");

        // Assert
        ManagedIdentityOptions miOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<ManagedIdentityOptions>>()
            .Get("CustomAuth");

        Assert.NotNull(miOptions);
        Assert.Equal("custom-umi-client-id", miOptions.UserAssignedClientId);
    }

    [Fact]
    public void AddBotApplication_WithMultipleSections_IsolatesManagedIdentityPerSection()
    {
        // Arrange: one UMI section and one app-secret section in the same host.
        Dictionary<string, string?> configData = new()
        {
            // UMI bot — no ClientCredentials
            ["UmiBot:ClientId"] = "umi-client-id",
            ["UmiBot:TenantId"] = "umi-tenant-id",

            // App-secret bot — has ClientCredentials, must NOT be classified as UMI
            ["SecretBot:ClientId"] = "secret-client-id",
            ["SecretBot:TenantId"] = "secret-tenant-id",
            ["SecretBot:ClientCredentials:0:SourceType"] = "ClientSecret",
            ["SecretBot:ClientCredentials:0:ClientSecret"] = "secret-value"
        };

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddBotApplication("UmiBot");
        services.AddBotApplication("SecretBot");

        // Act
        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IOptionsMonitor<ManagedIdentityOptions> monitor = serviceProvider
            .GetRequiredService<IOptionsMonitor<ManagedIdentityOptions>>();

        // Assert: UMI section gets its own ClientId; app-secret section is untouched.
        Assert.Equal("umi-client-id", monitor.Get("UmiBot").UserAssignedClientId);
        Assert.Null(monitor.Get("SecretBot").UserAssignedClientId);
    }

    [Fact]
    public void AddBotApplication_WithNestedSectionPath_ConfiguresOptionsUnderFullSectionName()
    {
        // Arrange: Nested section path (e.g., "Auth:AzureAd")
        Dictionary<string, string?> configData = new()
        {
            ["Auth:AzureAd:ClientId"] = "nested-client-id",
            ["Auth:AzureAd:TenantId"] = "nested-tenant-id"
        };

        // Act
        ServiceProvider serviceProvider = BuildServiceProviderWithManagedIdentity(configData, "Auth:AzureAd");

        // Assert: Verify MSAL options are configured under the full section name (not just the leaf key)
        MicrosoftIdentityApplicationOptions msalOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>()
            .Get("Auth:AzureAd");

        Assert.Equal("nested-client-id", msalOptions.ClientId);
        Assert.Equal("nested-tenant-id", msalOptions.TenantId);
    }
}
