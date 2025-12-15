// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Core.Hosting;
using Microsoft.Bot.Core.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Moq;

namespace Microsoft.Bot.Core.UnitTests.Hosting;

public class AddBotApplicationExtensionsTests
{
    private static ServiceProvider BuildServiceProvider(Dictionary<string, string?> configData, string? aadConfigSectionName = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var services = new ServiceCollection();
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

    private static void AssertMsalOptions(ServiceProvider serviceProvider, string expectedClientId, string expectedTenantId, string expectedInstance = "https://login.microsoftonline.com/")
    {
        var msalOptions = serviceProvider.GetRequiredService<IOptions<MicrosoftIdentityApplicationOptions>>().Value;
        Assert.Equal(expectedClientId, msalOptions.ClientId);
        Assert.Equal(expectedTenantId, msalOptions.TenantId);
        Assert.Equal(expectedInstance, msalOptions.Instance);
    }

    [Fact]
    public void AddConversationClient_WithBotFrameworkConfig_ConfiguresClientSecret()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["MicrosoftAppId"] = "test-app-id",
            ["MicrosoftAppTenantId"] = "test-tenant-id",
            ["MicrosoftAppPassword"] = "test-secret"
        };

        // Act
        var serviceProvider = BuildServiceProvider(configData);

        // Assert
        AssertMsalOptions(serviceProvider, "test-app-id", "test-tenant-id");
        var msalOptions = serviceProvider.GetRequiredService<IOptions<MicrosoftIdentityApplicationOptions>>().Value;
        Assert.NotNull(msalOptions.ClientCredentials);
        Assert.Single(msalOptions.ClientCredentials);
        var credential = msalOptions.ClientCredentials.First();
        Assert.Equal(CredentialSource.ClientSecret, credential.SourceType);
        Assert.Equal("test-secret", credential.ClientSecret);
    }

    [Fact]
    public void AddConversationClient_WithCoreConfigAndClientSecret_ConfiguresClientSecret()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["CLIENT_ID"] = "test-client-id",
            ["TENANT_ID"] = "test-tenant-id",
            ["CLIENT_SECRET"] = "test-client-secret"
        };

        // Act
        var serviceProvider = BuildServiceProvider(configData);

        // Assert
        AssertMsalOptions(serviceProvider, "test-client-id", "test-tenant-id");
        var msalOptions = serviceProvider.GetRequiredService<IOptions<MicrosoftIdentityApplicationOptions>>().Value;
        Assert.NotNull(msalOptions.ClientCredentials);
        Assert.Single(msalOptions.ClientCredentials);
        var credential = msalOptions.ClientCredentials.First();
        Assert.Equal(CredentialSource.ClientSecret, credential.SourceType);
        Assert.Equal("test-client-secret", credential.ClientSecret);
    }

    [Fact]
    public void AddConversationClient_WithCoreConfigAndSystemAssignedMI_ConfiguresSystemAssignedFIC()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["CLIENT_ID"] = "test-client-id",
            ["TENANT_ID"] = "test-tenant-id",
            ["MANAGED_IDENTITY_CLIENT_ID"] = "system"
        };

        // Act
        var serviceProvider = BuildServiceProvider(configData);

        // Assert
        AssertMsalOptions(serviceProvider, "test-client-id", "test-tenant-id");
        var msalOptions = serviceProvider.GetRequiredService<IOptions<MicrosoftIdentityApplicationOptions>>().Value;
        Assert.NotNull(msalOptions.ClientCredentials);
        Assert.Single(msalOptions.ClientCredentials);
        var credential = msalOptions.ClientCredentials.First();
        Assert.Equal(CredentialSource.SignedAssertionFromManagedIdentity, credential.SourceType);
        Assert.Null(credential.ManagedIdentityClientId); // System-assigned

        var managedIdentityOptions = serviceProvider.GetRequiredService<IOptions<ManagedIdentityOptions>>().Value;
        Assert.Null(managedIdentityOptions.UserAssignedClientId);
    }

    [Fact]
    public void AddConversationClient_WithCoreConfigAndUserAssignedMI_ConfiguresUserAssignedFIC()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["CLIENT_ID"] = "test-client-id",
            ["TENANT_ID"] = "test-tenant-id",
            ["MANAGED_IDENTITY_CLIENT_ID"] = "umi-client-id"  // Different from CLIENT_ID means FIC
        };

        // Act
        var serviceProvider = BuildServiceProvider(configData);

        // Assert
        AssertMsalOptions(serviceProvider, "test-client-id", "test-tenant-id");
        var msalOptions = serviceProvider.GetRequiredService<IOptions<MicrosoftIdentityApplicationOptions>>().Value;
        Assert.NotNull(msalOptions.ClientCredentials);
        Assert.Single(msalOptions.ClientCredentials);
        var credential = msalOptions.ClientCredentials.First();
        Assert.Equal(CredentialSource.SignedAssertionFromManagedIdentity, credential.SourceType);
        Assert.Equal("umi-client-id", credential.ManagedIdentityClientId);

        var managedIdentityOptions = serviceProvider.GetRequiredService<IOptions<ManagedIdentityOptions>>().Value;
        Assert.Null(managedIdentityOptions.UserAssignedClientId);
    }

    [Fact]
    public void AddConversationClient_WithCoreConfigAndNoManagedIdentity_ConfiguresUMIWithClientId()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["CLIENT_ID"] = "test-client-id",
            ["TENANT_ID"] = "test-tenant-id"
        };

        // Act
        var serviceProvider = BuildServiceProvider(configData);

        // Assert
        AssertMsalOptions(serviceProvider, "test-client-id", "test-tenant-id");
        var msalOptions = serviceProvider.GetRequiredService<IOptions<MicrosoftIdentityApplicationOptions>>().Value;
        Assert.Null(msalOptions.ClientCredentials);

        var managedIdentityOptions = serviceProvider.GetRequiredService<IOptions<ManagedIdentityOptions>>().Value;
        Assert.Equal("test-client-id", managedIdentityOptions.UserAssignedClientId);
    }

    [Fact]
    public void AddConversationClient_WithDefaultSection_ConfiguresFromSection()
    {
        // AzureAd is the default Section Name
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["AzureAd:ClientId"] = "azuread-client-id",
            ["AzureAd:TenantId"] = "azuread-tenant-id",
            ["AzureAd:Instance"] = "https://login.microsoftonline.com/"
        };

        // Act
        var serviceProvider = BuildServiceProvider(configData);

        // Assert
        AssertMsalOptions(serviceProvider, "azuread-client-id", "azuread-tenant-id");
    }

    [Fact]
    public void AddConversationClient_WithCustomSectionName_ConfiguresFromCustomSection()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["CustomAuth:ClientId"] = "custom-client-id",
            ["CustomAuth:TenantId"] = "custom-tenant-id",
            ["CustomAuth:Instance"] = "https://login.microsoftonline.com/"
        };

        // Act
        var serviceProvider = BuildServiceProvider(configData, "CustomAuth");

        // Assert
        AssertMsalOptions(serviceProvider, "custom-client-id", "custom-tenant-id");
    }
}
