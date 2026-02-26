// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Auth;

namespace Microsoft.Teams.Api.Tests.Auth;

public class CloudEnvironmentTests
{
    [Fact]
    public void Public_HasCorrectEndpoints()
    {
        var env = CloudEnvironment.Public;

        Assert.Equal("https://login.microsoftonline.com", env.LoginEndpoint);
        Assert.Equal("botframework.com", env.LoginTenant);
        Assert.Equal("https://api.botframework.com/.default", env.BotScope);
        Assert.Equal("https://token.botframework.com", env.TokenServiceUrl);
        Assert.Equal("https://login.botframework.com/v1/.well-known/openidconfiguration", env.OpenIdMetadataUrl);
        Assert.Equal("https://api.botframework.com", env.TokenIssuer);
        Assert.Equal("", env.ChannelService);
        Assert.Equal("https://token.botframework.com/.auth/web/redirect", env.OAuthRedirectUrl);
    }

    [Fact]
    public void USGov_HasCorrectEndpoints()
    {
        var env = CloudEnvironment.USGov;

        Assert.Equal("https://login.microsoftonline.us", env.LoginEndpoint);
        Assert.Equal("MicrosoftServices.onmicrosoft.us", env.LoginTenant);
        Assert.Equal("https://api.botframework.us/.default", env.BotScope);
        Assert.Equal("https://tokengcch.botframework.azure.us", env.TokenServiceUrl);
        Assert.Equal("https://login.botframework.azure.us/v1/.well-known/openidconfiguration", env.OpenIdMetadataUrl);
        Assert.Equal("https://api.botframework.us", env.TokenIssuer);
        Assert.Equal("https://botframework.azure.us", env.ChannelService);
        Assert.Equal("https://tokengcch.botframework.azure.us/.auth/web/redirect", env.OAuthRedirectUrl);
    }

    [Fact]
    public void USGovDoD_HasCorrectEndpoints()
    {
        var env = CloudEnvironment.USGovDoD;

        Assert.Equal("https://login.microsoftonline.us", env.LoginEndpoint);
        Assert.Equal("MicrosoftServices.onmicrosoft.us", env.LoginTenant);
        Assert.Equal("https://api.botframework.us/.default", env.BotScope);
        Assert.Equal("https://apiDoD.botframework.azure.us", env.TokenServiceUrl);
        Assert.Equal("https://login.botframework.azure.us/v1/.well-known/openidconfiguration", env.OpenIdMetadataUrl);
        Assert.Equal("https://api.botframework.us", env.TokenIssuer);
        Assert.Equal("https://botframework.azure.us", env.ChannelService);
        Assert.Equal("https://apiDoD.botframework.azure.us/.auth/web/redirect", env.OAuthRedirectUrl);
    }

    [Fact]
    public void China_HasCorrectEndpoints()
    {
        var env = CloudEnvironment.China;

        Assert.Equal("https://login.partner.microsoftonline.cn", env.LoginEndpoint);
        Assert.Equal("microsoftservices.partner.onmschina.cn", env.LoginTenant);
        Assert.Equal("https://api.botframework.azure.cn/.default", env.BotScope);
        Assert.Equal("https://token.botframework.azure.cn", env.TokenServiceUrl);
        Assert.Equal("https://login.botframework.azure.cn/v1/.well-known/openidconfiguration", env.OpenIdMetadataUrl);
        Assert.Equal("https://api.botframework.azure.cn", env.TokenIssuer);
        Assert.Equal("https://botframework.azure.cn", env.ChannelService);
        Assert.Equal("https://token.botframework.azure.cn/.auth/web/redirect", env.OAuthRedirectUrl);
    }

    [Theory]
    [InlineData("Public", "https://login.microsoftonline.com")]
    [InlineData("public", "https://login.microsoftonline.com")]
    [InlineData("PUBLIC", "https://login.microsoftonline.com")]
    [InlineData("USGov", "https://login.microsoftonline.us")]
    [InlineData("usgov", "https://login.microsoftonline.us")]
    [InlineData("USGovDoD", "https://login.microsoftonline.us")]
    [InlineData("usgovdod", "https://login.microsoftonline.us")]
    [InlineData("China", "https://login.partner.microsoftonline.cn")]
    [InlineData("china", "https://login.partner.microsoftonline.cn")]
    public void FromName_ResolvesCorrectly(string name, string expectedLoginEndpoint)
    {
        var env = CloudEnvironment.FromName(name);
        Assert.Equal(expectedLoginEndpoint, env.LoginEndpoint);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("")]
    [InlineData("Azure")]
    public void FromName_ThrowsForUnknownName(string name)
    {
        Assert.Throws<ArgumentException>(() => CloudEnvironment.FromName(name));
    }

    [Fact]
    public void FromName_ReturnsStaticInstances()
    {
        Assert.Same(CloudEnvironment.Public, CloudEnvironment.FromName("Public"));
        Assert.Same(CloudEnvironment.USGov, CloudEnvironment.FromName("USGov"));
        Assert.Same(CloudEnvironment.USGovDoD, CloudEnvironment.FromName("USGovDoD"));
        Assert.Same(CloudEnvironment.China, CloudEnvironment.FromName("China"));
    }

    [Fact]
    public void WithOverrides_AllNulls_ReturnsSameInstance()
    {
        var env = CloudEnvironment.Public;

        var result = env.WithOverrides();

        Assert.Same(env, result);
    }

    [Fact]
    public void WithOverrides_SingleOverride_ReplacesOnlyThatProperty()
    {
        var env = CloudEnvironment.Public;

        var result = env.WithOverrides(loginTenant: "my-tenant-id");

        Assert.NotSame(env, result);
        Assert.Equal("my-tenant-id", result.LoginTenant);
        Assert.Equal(env.LoginEndpoint, result.LoginEndpoint);
        Assert.Equal(env.BotScope, result.BotScope);
        Assert.Equal(env.TokenServiceUrl, result.TokenServiceUrl);
        Assert.Equal(env.OpenIdMetadataUrl, result.OpenIdMetadataUrl);
        Assert.Equal(env.TokenIssuer, result.TokenIssuer);
        Assert.Equal(env.ChannelService, result.ChannelService);
        Assert.Equal(env.OAuthRedirectUrl, result.OAuthRedirectUrl);
    }

    [Fact]
    public void WithOverrides_MultipleOverrides_ReplacesCorrectProperties()
    {
        var env = CloudEnvironment.China;

        var result = env.WithOverrides(
            loginEndpoint: "https://custom.login.cn",
            loginTenant: "custom-tenant",
            tokenServiceUrl: "https://custom.token.cn"
        );

        Assert.Equal("https://custom.login.cn", result.LoginEndpoint);
        Assert.Equal("custom-tenant", result.LoginTenant);
        Assert.Equal("https://custom.token.cn", result.TokenServiceUrl);
        // unchanged
        Assert.Equal(env.BotScope, result.BotScope);
        Assert.Equal(env.OpenIdMetadataUrl, result.OpenIdMetadataUrl);
        Assert.Equal(env.TokenIssuer, result.TokenIssuer);
        Assert.Equal(env.ChannelService, result.ChannelService);
        Assert.Equal(env.OAuthRedirectUrl, result.OAuthRedirectUrl);
    }

    [Fact]
    public void WithOverrides_AllOverrides_ReplacesAllProperties()
    {
        var env = CloudEnvironment.Public;

        var result = env.WithOverrides(
            loginEndpoint: "a",
            loginTenant: "b",
            botScope: "c",
            tokenServiceUrl: "d",
            openIdMetadataUrl: "e",
            tokenIssuer: "f",
            channelService: "g",
            oauthRedirectUrl: "h"
        );

        Assert.Equal("a", result.LoginEndpoint);
        Assert.Equal("b", result.LoginTenant);
        Assert.Equal("c", result.BotScope);
        Assert.Equal("d", result.TokenServiceUrl);
        Assert.Equal("e", result.OpenIdMetadataUrl);
        Assert.Equal("f", result.TokenIssuer);
        Assert.Equal("g", result.ChannelService);
        Assert.Equal("h", result.OAuthRedirectUrl);
    }
}
