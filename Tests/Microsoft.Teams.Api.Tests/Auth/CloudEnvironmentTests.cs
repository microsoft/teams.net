// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Auth;

namespace Microsoft.Teams.Api.Tests.Auth;

public class CloudEnvironmentTests
{
    [Fact]
    public void DefaultConstructor_HasPublicCloudEndpoints()
    {
        var env = new CloudEnvironment();

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
    public void Constructor_AcceptsCustomEndpoints()
    {
        var env = new CloudEnvironment(
            loginEndpoint: "https://custom.login.example",
            loginTenant: "custom-tenant",
            botScope: "https://custom.scope/.default",
            tokenServiceUrl: "https://custom.token.example",
            openIdMetadataUrl: "https://custom.openid.example",
            tokenIssuer: "https://custom.issuer.example",
            channelService: "https://custom.channel.example",
            oauthRedirectUrl: "https://custom.redirect.example"
        );

        Assert.Equal("https://custom.login.example", env.LoginEndpoint);
        Assert.Equal("custom-tenant", env.LoginTenant);
        Assert.Equal("https://custom.scope/.default", env.BotScope);
        Assert.Equal("https://custom.token.example", env.TokenServiceUrl);
        Assert.Equal("https://custom.openid.example", env.OpenIdMetadataUrl);
        Assert.Equal("https://custom.issuer.example", env.TokenIssuer);
        Assert.Equal("https://custom.channel.example", env.ChannelService);
        Assert.Equal("https://custom.redirect.example", env.OAuthRedirectUrl);
    }

    [Fact]
    public void WithOverrides_AllNulls_ReturnsSameInstance()
    {
        var env = new CloudEnvironment();

        var result = env.WithOverrides();

        Assert.Same(env, result);
    }

    [Fact]
    public void WithOverrides_SingleOverride_ReplacesOnlyThatProperty()
    {
        var env = new CloudEnvironment();

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
        var env = new CloudEnvironment();

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
        var env = new CloudEnvironment();

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
