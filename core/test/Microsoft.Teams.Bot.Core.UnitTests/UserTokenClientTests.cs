// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Teams.Bot.Core;

namespace Microsoft.Teams.Bot.Core.UnitTests;

public class UserTokenClientTests
{
    private static IConfiguration Config(Dictionary<string, string?> data) =>
        new ConfigurationBuilder().AddInMemoryCollection(data).Build();

    [Fact]
    public void ResolveApiEndpoint_NoConfiguration_DefaultsToPublicTokenService()
    {
        Assert.Equal("https://token.botframework.com", UserTokenClient.ResolveApiEndpoint(Config([])));
    }

    [Fact]
    public void ResolveApiEndpoint_ExplicitUserTokenApiEndpoint_WinsOverEverything()
    {
        IConfiguration config = Config(new()
        {
            ["UserTokenApiEndpoint"] = "https://my.explicit.endpoint",
            ["AzureAd:Cloud"] = "USGov",
            ["AzureAd:TokenServiceUrl"] = "https://should-be-ignored"
        });

        Assert.Equal("https://my.explicit.endpoint", UserTokenClient.ResolveApiEndpoint(config));
    }

    [Theory]
    [InlineData("USGov", "https://tokengcch.botframework.azure.us")]
    [InlineData("USGovDoD", "https://apiDoD.botframework.azure.us")]
    [InlineData("China", "https://token.botframework.azure.cn")]
    [InlineData("Public", "https://token.botframework.com")]
    public void ResolveApiEndpoint_CloudPresetAzureAdSection_ResolvesSovereignEndpoint(string cloudName, string expected)
    {
        IConfiguration config = Config(new() { ["AzureAd:Cloud"] = cloudName });
        Assert.Equal(expected, UserTokenClient.ResolveApiEndpoint(config));
    }

    [Fact]
    public void ResolveApiEndpoint_RootCloudKey_Works()
    {
        Assert.Equal("https://tokengcch.botframework.azure.us",
            UserTokenClient.ResolveApiEndpoint(Config(new() { ["Cloud"] = "USGov" })));
    }

    [Fact]
    public void ResolveApiEndpoint_UppercaseCloudKey_Works()
    {
        Assert.Equal("https://token.botframework.azure.cn",
            UserTokenClient.ResolveApiEndpoint(Config(new() { ["CLOUD"] = "China" })));
    }

    [Fact]
    public void ResolveApiEndpoint_SectionTokenServiceUrlOverride_AppliesOnTopOfCloud()
    {
        IConfiguration config = Config(new()
        {
            ["AzureAd:Cloud"] = "USGov",
            ["AzureAd:TokenServiceUrl"] = "https://custom.token.service"
        });

        Assert.Equal("https://custom.token.service", UserTokenClient.ResolveApiEndpoint(config));
    }

    [Fact]
    public void ResolveApiEndpoint_RootTokenServiceUrlOverride_AppliesWhenNoCloudSet()
    {
        IConfiguration config = Config(new() { ["TokenServiceUrl"] = "https://root-override" });

        Assert.Equal("https://root-override", UserTokenClient.ResolveApiEndpoint(config));
    }

    [Fact]
    public void ResolveApiEndpoint_SectionOverrideBeatsRootOverride()
    {
        IConfiguration config = Config(new()
        {
            ["AzureAd:TokenServiceUrl"] = "https://section",
            ["TokenServiceUrl"] = "https://root"
        });

        Assert.Equal("https://section", UserTokenClient.ResolveApiEndpoint(config));
    }

    [Fact]
    public void ResolveApiEndpoint_WhitespaceOverrideIgnored_FallsBackToCloud()
    {
        IConfiguration config = Config(new()
        {
            ["AzureAd:Cloud"] = "USGov",
            ["AzureAd:TokenServiceUrl"] = "   "
        });

        Assert.Equal("https://tokengcch.botframework.azure.us", UserTokenClient.ResolveApiEndpoint(config));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ResolveApiEndpoint_EmptyOrWhitespaceCloud_DefaultsToPublic(string cloudValue)
    {
        IConfiguration config = Config(new() { ["AzureAd:Cloud"] = cloudValue });
        Assert.Equal("https://token.botframework.com", UserTokenClient.ResolveApiEndpoint(config));
    }
}
