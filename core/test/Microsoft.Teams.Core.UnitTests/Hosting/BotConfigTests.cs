// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.Core.Hosting;

namespace Microsoft.Teams.Core.UnitTests.Hosting;

public class BotConfigTests
{
    private static ServiceCollection BuildServices(Dictionary<string, string?> configData)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        return services;
    }

    [Fact]
    public void Resolve_OpenIdMetadataUrl_DefaultsToPublicCloud_WhenNotConfigured()
    {
        ServiceCollection services = BuildServices(new Dictionary<string, string?>
        {
            ["AzureAd:ClientId"] = "client-id",
            ["AzureAd:TenantId"] = "tenant-id",
        });

        BotConfig config = BotConfig.Resolve(services);

        Assert.Equal("https://login.botframework.com/v1/.well-known/openid-configuration", config.OpenIdMetadataUrl);
    }

    [Fact]
    public void Resolve_EntraInstance_DefaultsToPublicCloud_WhenNotConfigured()
    {
        ServiceCollection services = BuildServices(new Dictionary<string, string?>
        {
            ["AzureAd:ClientId"] = "client-id",
            ["AzureAd:TenantId"] = "tenant-id",
        });

        BotConfig config = BotConfig.Resolve(services);

        Assert.Equal("https://login.microsoftonline.com/", config.EntraInstance);
    }

    [Theory]
    [InlineData("https://login.botframework.azure.us/v1/.well-known/openid-configuration")]
    [InlineData("https://login.botframework.azure.cn/v1/.well-known/openid-configuration")]
    public void Resolve_OpenIdMetadataUrl_HonorsBotFrameworkOverride(string configured)
    {
        ServiceCollection services = BuildServices(new Dictionary<string, string?>
        {
            ["AzureAd:ClientId"] = "client-id",
            ["AzureAd:TenantId"] = "tenant-id",
            ["BotFramework:OpenIdMetadataUrl"] = configured,
        });

        BotConfig config = BotConfig.Resolve(services);

        Assert.Equal(configured, config.OpenIdMetadataUrl);
    }

    [Theory]
    [InlineData("https://login.microsoftonline.us/")]
    [InlineData("https://login.partner.microsoftonline.cn/")]
    public void Resolve_EntraInstance_HonorsAzureAdInstanceOverride(string configured)
    {
        ServiceCollection services = BuildServices(new Dictionary<string, string?>
        {
            ["AzureAd:ClientId"] = "client-id",
            ["AzureAd:TenantId"] = "tenant-id",
            ["AzureAd:Instance"] = configured,
        });

        BotConfig config = BotConfig.Resolve(services);

        Assert.Equal(configured, config.EntraInstance);
    }

    [Fact]
    public void Resolve_BotTokenIssuer_DefaultsToPublicCloud_WhenNotConfigured()
    {
        ServiceCollection services = BuildServices(new Dictionary<string, string?>
        {
            ["AzureAd:ClientId"] = "client-id",
            ["AzureAd:TenantId"] = "tenant-id",
        });

        BotConfig config = BotConfig.Resolve(services);

        Assert.Equal("https://api.botframework.com", config.BotTokenIssuer);
    }

    [Theory]
    [InlineData("https://api.botframework.us")]
    [InlineData("https://api.botframework.azure.cn")]
    public void Resolve_BotTokenIssuer_HonorsBotFrameworkOverride(string configured)
    {
        ServiceCollection services = BuildServices(new Dictionary<string, string?>
        {
            ["AzureAd:ClientId"] = "client-id",
            ["AzureAd:TenantId"] = "tenant-id",
            ["BotFramework:BotTokenIssuer"] = configured,
        });

        BotConfig config = BotConfig.Resolve(services);

        Assert.Equal(configured, config.BotTokenIssuer);
    }

    [Fact]
    public void Resolve_BotFrameworkSection_IsIndependentOfAzureAdSectionName()
    {
        ServiceCollection services = BuildServices(new Dictionary<string, string?>
        {
            ["CustomAuth:ClientId"] = "client-id",
            ["BotFramework:OpenIdMetadataUrl"] = "https://login.botframework.azure.us/v1/.well-known/openid-configuration",
            ["BotFramework:BotTokenIssuer"] = "https://api.botframework.us",
        });

        BotConfig config = BotConfig.Resolve(services, "CustomAuth");

        Assert.Equal("https://login.botframework.azure.us/v1/.well-known/openid-configuration", config.OpenIdMetadataUrl);
        Assert.Equal("https://api.botframework.us", config.BotTokenIssuer);
    }

    [Fact]
    public void Resolve_ThrowsInvalidOperationException_WhenOpenIdMetadataUrlIsNotAbsoluteUri()
    {
        ServiceCollection services = BuildServices(new Dictionary<string, string?>
        {
            ["AzureAd:ClientId"] = "client-id",
            ["BotFramework:OpenIdMetadataUrl"] = "not-a-uri",
        });

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => BotConfig.Resolve(services));
        Assert.Contains("BotFramework:OpenIdMetadataUrl", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolve_ThrowsInvalidOperationException_WhenBotTokenIssuerIsNotAbsoluteUri()
    {
        ServiceCollection services = BuildServices(new Dictionary<string, string?>
        {
            ["AzureAd:ClientId"] = "client-id",
            ["BotFramework:BotTokenIssuer"] = "not a uri",
        });

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => BotConfig.Resolve(services));
        Assert.Contains("BotFramework:BotTokenIssuer", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolve_ThrowsInvalidOperationException_WhenInstanceIsNotAbsoluteUri()
    {
        ServiceCollection services = BuildServices(new Dictionary<string, string?>
        {
            ["AzureAd:ClientId"] = "client-id",
            ["AzureAd:Instance"] = "login.microsoftonline.us",
        });

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => BotConfig.Resolve(services));
        Assert.Contains("AzureAd:Instance", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolve_DoesNotThrow_WhenOverridesAreValidAbsoluteUris()
    {
        ServiceCollection services = BuildServices(new Dictionary<string, string?>
        {
            ["AzureAd:ClientId"] = "client-id",
            ["AzureAd:Instance"] = "https://login.microsoftonline.us/",
            ["BotFramework:OpenIdMetadataUrl"] = "https://login.botframework.azure.us/v1/.well-known/openid-configuration",
            ["BotFramework:BotTokenIssuer"] = "https://api.botframework.us",
        });

        Exception? caught = Record.Exception(() => BotConfig.Resolve(services));

        Assert.Null(caught);
    }

    [Fact]
    public void Resolve_WithLegacyTeamsSection_ResolvesClientIdAndTenantId()
    {
        ServiceCollection services = BuildServices(new Dictionary<string, string?>
        {
            ["Teams:ClientId"] = "legacy-client-id",
            ["Teams:TenantId"] = "legacy-tenant-id",
            ["Teams:ClientSecret"] = "legacy-secret",
        });

        BotConfig config = BotConfig.Resolve(services);

        Assert.Equal("legacy-client-id", config.ClientId);
        Assert.Equal("legacy-tenant-id", config.TenantId);
    }

    [Fact]
    public void Resolve_WithLegacyTeamsSection_DefaultsEntraInstanceToPublicCloud()
    {
        ServiceCollection services = BuildServices(new Dictionary<string, string?>
        {
            ["Teams:ClientId"] = "legacy-client-id",
            ["Teams:TenantId"] = "legacy-tenant-id",
            ["Teams:ClientSecret"] = "legacy-secret",
        });

        BotConfig config = BotConfig.Resolve(services);

        Assert.Equal("https://login.microsoftonline.com/", config.EntraInstance);
    }

    [Fact]
    public void Resolve_WithLegacyTeamsSection_SectionNameRemainsAzureAd()
    {
        ServiceCollection services = BuildServices(new Dictionary<string, string?>
        {
            ["Teams:ClientId"] = "legacy-client-id",
            ["Teams:TenantId"] = "legacy-tenant-id",
            ["Teams:ClientSecret"] = "legacy-secret",
        });

        BotConfig config = BotConfig.Resolve(services);

        Assert.Equal("AzureAd", config.SectionName);
    }

    [Fact]
    public void Resolve_WithLegacyTeamsSection_MsalSectionHasClientCredentialsShape()
    {
        ServiceCollection services = BuildServices(new Dictionary<string, string?>
        {
            ["Teams:ClientId"] = "legacy-client-id",
            ["Teams:TenantId"] = "legacy-tenant-id",
            ["Teams:ClientSecret"] = "legacy-secret",
        });

        BotConfig config = BotConfig.Resolve(services);

        Assert.NotNull(config.MsalConfigurationSection);
        Assert.Equal("ClientSecret", config.MsalConfigurationSection["ClientCredentials:0:SourceType"]);
        Assert.Equal("legacy-secret", config.MsalConfigurationSection["ClientCredentials:0:ClientSecret"]);
    }

    [Fact]
    public void Resolve_WithLegacyTeamsSection_IsNotUserAssignedManagedIdentity()
    {
        ServiceCollection services = BuildServices(new Dictionary<string, string?>
        {
            ["Teams:ClientId"] = "legacy-client-id",
            ["Teams:TenantId"] = "legacy-tenant-id",
            ["Teams:ClientSecret"] = "legacy-secret",
        });

        BotConfig config = BotConfig.Resolve(services);

        Assert.False(config.IsUserAssignedManagedIdentity);
    }

    [Fact]
    public void Resolve_WhenBothSectionsExist_AzureAdTakesPrecedence()
    {
        ServiceCollection services = BuildServices(new Dictionary<string, string?>
        {
            ["AzureAd:ClientId"] = "new-client-id",
            ["AzureAd:TenantId"] = "new-tenant-id",
            ["AzureAd:ClientCredentials:0:SourceType"] = "ClientSecret",
            ["AzureAd:ClientCredentials:0:ClientSecret"] = "new-secret",
            ["Teams:ClientId"] = "legacy-client-id",
            ["Teams:TenantId"] = "legacy-tenant-id",
            ["Teams:ClientSecret"] = "legacy-secret",
        });

        BotConfig config = BotConfig.Resolve(services);

        Assert.Equal("new-client-id", config.ClientId);
        Assert.Equal("new-tenant-id", config.TenantId);
    }

    [Fact]
    public void Resolve_WithLegacyTeamsSection_MsalSectionHasCorrectTenantIdAndClientId()
    {
        ServiceCollection services = BuildServices(new Dictionary<string, string?>
        {
            ["Teams:ClientId"] = "legacy-client-id",
            ["Teams:TenantId"] = "legacy-tenant-id",
            ["Teams:ClientSecret"] = "legacy-secret",
        });

        BotConfig config = BotConfig.Resolve(services);

        Assert.NotNull(config.MsalConfigurationSection);
        Assert.Equal("legacy-client-id", config.MsalConfigurationSection["ClientId"]);
        Assert.Equal("legacy-tenant-id", config.MsalConfigurationSection["TenantId"]);
        Assert.Equal("https://login.microsoftonline.com/", config.MsalConfigurationSection["Instance"]);
    }
}
