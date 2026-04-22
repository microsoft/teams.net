// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Teams.Bot.Core.Hosting;

namespace Microsoft.Teams.Bot.Core.UnitTests.Hosting;

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

    private static void AssertMsalOptions(ServiceProvider serviceProvider, string expectedClientId, string expectedTenantId, string expectedInstance = "https://login.microsoftonline.com/")
    {
        MicrosoftIdentityApplicationOptions msalOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>()
            .Get(MsalConfigurationExtensions.MsalConfigKey);
        Assert.Equal(expectedClientId, msalOptions.ClientId);
        Assert.Equal(expectedTenantId, msalOptions.TenantId);
        Assert.Equal(expectedInstance, msalOptions.Instance);
    }

    [Fact]
    public void AddConversationClient_WithBotFrameworkConfig_ConfiguresClientSecret()
    {
        // Arrange
        Dictionary<string, string?> configData = new()
        {
            ["MicrosoftAppId"] = "test-app-id",
            ["MicrosoftAppTenantId"] = "test-tenant-id",
            ["MicrosoftAppPassword"] = "test-secret"
        };

        // Act
        ServiceProvider serviceProvider = BuildServiceProvider(configData);

        // Assert
        AssertMsalOptions(serviceProvider, "test-app-id", "test-tenant-id");
        MicrosoftIdentityApplicationOptions msalOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>()
            .Get(MsalConfigurationExtensions.MsalConfigKey);
        Assert.NotNull(msalOptions.ClientCredentials);
        Assert.Single(msalOptions.ClientCredentials);
        CredentialDescription credential = msalOptions.ClientCredentials.First();
        Assert.Equal(CredentialSource.ClientSecret, credential.SourceType);
        Assert.Equal("test-secret", credential.ClientSecret);
    }

    [Fact]
    public void AddConversationClient_WithCoreConfigAndClientSecret_ConfiguresClientSecret()
    {
        // Arrange
        Dictionary<string, string?> configData = new()
        {
            ["CLIENT_ID"] = "test-client-id",
            ["TENANT_ID"] = "test-tenant-id",
            ["CLIENT_SECRET"] = "test-client-secret"
        };

        // Act
        ServiceProvider serviceProvider = BuildServiceProvider(configData);

        // Assert
        AssertMsalOptions(serviceProvider, "test-client-id", "test-tenant-id");
        MicrosoftIdentityApplicationOptions msalOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>()
            .Get(MsalConfigurationExtensions.MsalConfigKey);
        Assert.NotNull(msalOptions.ClientCredentials);
        Assert.Single(msalOptions.ClientCredentials);
        CredentialDescription credential = msalOptions.ClientCredentials.First();
        Assert.Equal(CredentialSource.ClientSecret, credential.SourceType);
        Assert.Equal("test-client-secret", credential.ClientSecret);
    }

    [Fact]
    public void AddConversationClient_WithCoreConfigAndSystemAssignedMI_ConfiguresSystemAssignedFIC()
    {
        // Arrange
        Dictionary<string, string?> configData = new()
        {
            ["CLIENT_ID"] = "test-client-id",
            ["TENANT_ID"] = "test-tenant-id",
            ["MANAGED_IDENTITY_CLIENT_ID"] = "system"
        };

        // Act
        ServiceProvider serviceProvider = BuildServiceProvider(configData);

        // Assert
        AssertMsalOptions(serviceProvider, "test-client-id", "test-tenant-id");
        MicrosoftIdentityApplicationOptions msalOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>()
            .Get(MsalConfigurationExtensions.MsalConfigKey);
        Assert.NotNull(msalOptions.ClientCredentials);
        Assert.Single(msalOptions.ClientCredentials);
        CredentialDescription credential = msalOptions.ClientCredentials.First();
        Assert.Equal(CredentialSource.SignedAssertionFromManagedIdentity, credential.SourceType);
        Assert.Null(credential.ManagedIdentityClientId); // System-assigned

        ManagedIdentityOptions managedIdentityOptions = serviceProvider.GetRequiredService<IOptions<ManagedIdentityOptions>>().Value;
        Assert.Null(managedIdentityOptions.UserAssignedClientId);
    }

    [Fact]
    public void AddConversationClient_WithCoreConfigAndUserAssignedMI_ConfiguresUserAssignedFIC()
    {
        // Arrange
        Dictionary<string, string?> configData = new()
        {
            ["CLIENT_ID"] = "test-client-id",
            ["TENANT_ID"] = "test-tenant-id",
            ["MANAGED_IDENTITY_CLIENT_ID"] = "umi-client-id"  // Different from CLIENT_ID means FIC
        };

        // Act
        ServiceProvider serviceProvider = BuildServiceProvider(configData);

        // Assert
        AssertMsalOptions(serviceProvider, "test-client-id", "test-tenant-id");
        MicrosoftIdentityApplicationOptions msalOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>()
            .Get(MsalConfigurationExtensions.MsalConfigKey);
        Assert.NotNull(msalOptions.ClientCredentials);
        Assert.Single(msalOptions.ClientCredentials);
        CredentialDescription credential = msalOptions.ClientCredentials.First();
        Assert.Equal(CredentialSource.SignedAssertionFromManagedIdentity, credential.SourceType);
        Assert.Equal("umi-client-id", credential.ManagedIdentityClientId);

        ManagedIdentityOptions managedIdentityOptions = serviceProvider.GetRequiredService<IOptions<ManagedIdentityOptions>>().Value;
        Assert.Null(managedIdentityOptions.UserAssignedClientId);
    }

    [Fact]
    public void AddConversationClient_WithCoreConfigAndNoManagedIdentity_ConfiguresUMIWithClientId()
    {
        // Arrange
        Dictionary<string, string?> configData = new()
        {
            ["CLIENT_ID"] = "test-client-id",
            ["TENANT_ID"] = "test-tenant-id"
        };

        // Act
        ServiceProvider serviceProvider = BuildServiceProvider(configData);

        // Assert
        AssertMsalOptions(serviceProvider, "test-client-id", "test-tenant-id");
        MicrosoftIdentityApplicationOptions msalOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>()
            .Get(MsalConfigurationExtensions.MsalConfigKey);
        Assert.Null(msalOptions.ClientCredentials);

        ManagedIdentityOptions managedIdentityOptions = serviceProvider.GetRequiredService<IOptions<ManagedIdentityOptions>>().Value;
        Assert.Equal("test-client-id", managedIdentityOptions.UserAssignedClientId);
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
        AssertMsalOptions(serviceProvider, "azuread-client-id", "azuread-tenant-id");
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
        AssertMsalOptions(serviceProvider, "custom-client-id", "custom-tenant-id");
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
    public void AddBotApplication_WithMicrosoftAppId_SetsAppIdFromMicrosoftAppId()
    {
        // Arrange
        Dictionary<string, string?> configData = new()
        {
            ["MicrosoftAppId"] = "bf-app-id",
            ["MicrosoftAppTenantId"] = "bf-tenant-id"
        };

        // Act
        ServiceProvider serviceProvider = BuildServiceProviderForBotApp(configData);

        // Assert
        Assert.Equal("bf-app-id", GetAppId(serviceProvider));
    }

    [Fact]
    public void AddBotApplication_WithClientId_SetsAppIdFromClientId()
    {
        // Arrange
        Dictionary<string, string?> configData = new()
        {
            ["CLIENT_ID"] = "core-client-id",
            ["TENANT_ID"] = "core-tenant-id"
        };

        // Act
        ServiceProvider serviceProvider = BuildServiceProviderForBotApp(configData);

        // Assert
        Assert.Equal("core-client-id", GetAppId(serviceProvider));
    }

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

    [Fact]
    public void AddBotApplication_ClientIdTakesPrecedenceOverMicrosoftAppId()
    {
        // Arrange — both keys present; CLIENT_ID is highest priority
        Dictionary<string, string?> configData = new()
        {
            ["MicrosoftAppId"] = "bf-app-id",
            ["MicrosoftAppTenantId"] = "bf-tenant-id",
            ["CLIENT_ID"] = "core-client-id",
            ["TENANT_ID"] = "core-tenant-id"
        };

        // Act
        ServiceProvider serviceProvider = BuildServiceProviderForBotApp(configData);

        // Assert
        Assert.Equal("core-client-id", GetAppId(serviceProvider));
    }

    // --- MSAL Instance / Authority validation + Cloud fallback ---

    [Fact]
    public void AddConversationClient_SectionInstanceMatchesCloud_Succeeds()
    {
        Dictionary<string, string?> configData = new()
        {
            ["AzureAd:ClientId"] = "cid",
            ["AzureAd:TenantId"] = "tid",
            ["AzureAd:Cloud"] = "USGov",
            ["AzureAd:Instance"] = "https://login.microsoftonline.us/"
        };

        ServiceProvider serviceProvider = BuildServiceProvider(configData);

        AssertMsalOptions(serviceProvider, "cid", "tid", "https://login.microsoftonline.us/");
    }

    [Fact]
    public void AddConversationClient_SectionInstanceConflictsWithCloud_Throws()
    {
        Dictionary<string, string?> configData = new()
        {
            ["AzureAd:ClientId"] = "cid",
            ["AzureAd:TenantId"] = "tid",
            ["AzureAd:Cloud"] = "USGov",
            ["AzureAd:Instance"] = "https://login.microsoftonline.com/"
        };

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => BuildServiceProvider(configData));
        Assert.Contains("AzureAd:Instance", ex.Message);
        Assert.Contains("https://login.microsoftonline.com/", ex.Message);
        Assert.Contains("https://login.microsoftonline.us", ex.Message);
    }

    [Fact]
    public void AddConversationClient_SectionInstanceMissing_FallsBackToCloud()
    {
        Dictionary<string, string?> configData = new()
        {
            ["AzureAd:ClientId"] = "cid",
            ["AzureAd:TenantId"] = "tid",
            ["AzureAd:Cloud"] = "USGov"
        };

        ServiceProvider serviceProvider = BuildServiceProvider(configData);

        AssertMsalOptions(serviceProvider, "cid", "tid", "https://login.microsoftonline.us/");
    }

    [Fact]
    public void AddConversationClient_SectionAuthorityMatchesCloud_Succeeds()
    {
        Dictionary<string, string?> configData = new()
        {
            ["AzureAd:ClientId"] = "cid",
            ["AzureAd:TenantId"] = "tid",
            ["AzureAd:Cloud"] = "USGov",
            ["AzureAd:Authority"] = "https://login.microsoftonline.us/tid/v2.0"
        };

        ServiceProvider serviceProvider = BuildServiceProvider(configData);

        MicrosoftIdentityApplicationOptions msalOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<MicrosoftIdentityApplicationOptions>>()
            .Get(MsalConfigurationExtensions.MsalConfigKey);
        Assert.Equal("cid", msalOptions.ClientId);
    }

    [Fact]
    public void AddConversationClient_SectionAuthorityConflictsWithCloud_Throws()
    {
        Dictionary<string, string?> configData = new()
        {
            ["AzureAd:ClientId"] = "cid",
            ["AzureAd:TenantId"] = "tid",
            ["AzureAd:Cloud"] = "USGov",
            ["AzureAd:Authority"] = "https://login.microsoftonline.com/tid/v2.0"
        };

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => BuildServiceProvider(configData));
        Assert.Contains("AzureAd:Authority", ex.Message);
        Assert.Contains("https://login.microsoftonline.com/tid/v2.0", ex.Message);
    }

    [Fact]
    public void AddConversationClient_DefaultCloud_InstanceDefaultsToPublic()
    {
        Dictionary<string, string?> configData = new()
        {
            ["AzureAd:ClientId"] = "cid",
            ["AzureAd:TenantId"] = "tid"
        };

        ServiceProvider serviceProvider = BuildServiceProvider(configData);

        AssertMsalOptions(serviceProvider, "cid", "tid", "https://login.microsoftonline.com/");
    }

    [Fact]
    public void AddConversationClient_RawCredentials_ConfiguresCloudAwareInstance()
    {
        // Core-config path (no MSAL section) should set MSAL Instance from Cloud.
        Dictionary<string, string?> configData = new()
        {
            ["CLIENT_ID"] = "cid",
            ["TENANT_ID"] = "tid",
            ["CLIENT_SECRET"] = "secret",
            ["CLOUD"] = "USGov"
        };

        ServiceProvider serviceProvider = BuildServiceProvider(configData);

        AssertMsalOptions(serviceProvider, "cid", "tid", "https://login.microsoftonline.us/");
    }

    // --- Per-endpoint override binding (BotConfig.ResolveCloud) ---

    [Fact]
    public void BotConfig_FromMsalConfig_AppliesTokenIssuerOverrideFromSection()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureAd:ClientId"] = "cid",
                ["AzureAd:Cloud"] = "USGov",
                ["AzureAd:TokenIssuer"] = "https://custom.issuer"
            })
            .Build();

        BotConfig result = BotConfig.FromMsalConfig(config);

        Assert.Equal("https://custom.issuer", result.Cloud.TokenIssuer);
        Assert.Equal(CloudEnvironment.USGov.LoginEndpoint, result.Cloud.LoginEndpoint);
    }

    [Fact]
    public void BotConfig_FromMsalConfig_AppliesMultipleEndpointOverrides()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureAd:ClientId"] = "cid",
                ["AzureAd:Cloud"] = "USGov",
                ["AzureAd:TokenIssuer"] = "https://iss",
                ["AzureAd:TokenServiceUrl"] = "https://tsu",
                ["AzureAd:GraphScope"] = "https://graph.override/.default"
            })
            .Build();

        BotConfig result = BotConfig.FromMsalConfig(config);

        Assert.Equal("https://iss", result.Cloud.TokenIssuer);
        Assert.Equal("https://tsu", result.Cloud.TokenServiceUrl);
        Assert.Equal("https://graph.override/.default", result.Cloud.GraphScope);
    }

    [Fact]
    public void BotConfig_FromMsalConfig_SectionOverrideWinsOverRoot()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureAd:ClientId"] = "cid",
                ["AzureAd:Cloud"] = "USGov",
                ["AzureAd:TokenIssuer"] = "https://section",
                ["TokenIssuer"] = "https://root"
            })
            .Build();

        BotConfig result = BotConfig.FromMsalConfig(config);

        Assert.Equal("https://section", result.Cloud.TokenIssuer);
    }

    [Fact]
    public void BotConfig_FromMsalConfig_FallsBackToRootOverride()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureAd:ClientId"] = "cid",
                ["AzureAd:Cloud"] = "USGov",
                ["TokenIssuer"] = "https://root"
            })
            .Build();

        BotConfig result = BotConfig.FromMsalConfig(config);

        Assert.Equal("https://root", result.Cloud.TokenIssuer);
    }

    [Fact]
    public void BotConfig_FromMsalConfig_WhitespaceOverrideIgnored()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureAd:ClientId"] = "cid",
                ["AzureAd:Cloud"] = "USGov",
                ["AzureAd:TokenIssuer"] = "   "
            })
            .Build();

        BotConfig result = BotConfig.FromMsalConfig(config);

        Assert.Equal(CloudEnvironment.USGov.TokenIssuer, result.Cloud.TokenIssuer);
    }

    [Fact]
    public void BotConfig_FromBFConfig_AppliesEndpointOverrides()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MicrosoftAppId"] = "cid",
                ["Cloud"] = "USGov",
                ["TokenIssuer"] = "https://bf-custom"
            })
            .Build();

        BotConfig result = BotConfig.FromBFConfig(config);

        Assert.Equal("https://bf-custom", result.Cloud.TokenIssuer);
    }

    [Fact]
    public void BotConfig_FromCoreConfig_AppliesEndpointOverrides()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CLIENT_ID"] = "cid",
                ["CLOUD"] = "China",
                ["TokenIssuer"] = "https://core-custom"
            })
            .Build();

        BotConfig result = BotConfig.FromCoreConfig(config);

        Assert.Equal("https://core-custom", result.Cloud.TokenIssuer);
        Assert.Equal(CloudEnvironment.China.LoginEndpoint, result.Cloud.LoginEndpoint);
    }
}
