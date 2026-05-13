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
    public void Resolve_OpenIdMetadataUrl_HonorsAzureAdOverride(string configured)
    {
        ServiceCollection services = BuildServices(new Dictionary<string, string?>
        {
            ["AzureAd:ClientId"] = "client-id",
            ["AzureAd:TenantId"] = "tenant-id",
            ["AzureAd:OpenIdMetadataUrl"] = configured,
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
    public void Resolve_OpenIdMetadataUrl_ReadsFromCustomSection_WhenSectionNameProvided()
    {
        ServiceCollection services = BuildServices(new Dictionary<string, string?>
        {
            ["CustomAuth:ClientId"] = "client-id",
            ["CustomAuth:OpenIdMetadataUrl"] = "https://login.botframework.azure.us/v1/.well-known/openid-configuration",
        });

        BotConfig config = BotConfig.Resolve(services, "CustomAuth");

        Assert.Equal("https://login.botframework.azure.us/v1/.well-known/openid-configuration", config.OpenIdMetadataUrl);
    }
}
