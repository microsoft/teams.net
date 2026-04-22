// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core.Hosting;

namespace Microsoft.Teams.Bot.Core.UnitTests.Hosting;

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
        Assert.Equal("https://graph.microsoft.com/.default", env.GraphScope);
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
        Assert.Equal("https://graph.microsoft.us/.default", env.GraphScope);
    }

    [Fact]
    public void USGovDoD_HasCorrectEndpoints()
    {
        var env = CloudEnvironment.USGovDoD;

        Assert.Equal("https://login.microsoftonline.us", env.LoginEndpoint);
        Assert.Equal("MicrosoftServices.onmicrosoft.us", env.LoginTenant);
        Assert.Equal("https://api.botframework.us/.default", env.BotScope);
        Assert.Equal("https://apiDoD.botframework.azure.us", env.TokenServiceUrl);
        Assert.Equal("https://api.botframework.us", env.TokenIssuer);
        Assert.Equal("https://dod-graph.microsoft.us/.default", env.GraphScope);
    }

    [Fact]
    public void China_HasCorrectEndpoints()
    {
        var env = CloudEnvironment.China;

        Assert.Equal("https://login.partner.microsoftonline.cn", env.LoginEndpoint);
        Assert.Equal("microsoftservices.partner.onmschina.cn", env.LoginTenant);
        Assert.Equal("https://api.botframework.azure.cn/.default", env.BotScope);
        Assert.Equal("https://token.botframework.azure.cn", env.TokenServiceUrl);
        Assert.Equal("https://api.botframework.azure.cn", env.TokenIssuer);
        Assert.Equal("https://microsoftgraph.chinacloudapi.cn/.default", env.GraphScope);
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

    [Fact]
    public void FromName_ReturnsStaticInstances()
    {
        Assert.Same(CloudEnvironment.Public, CloudEnvironment.FromName("Public"));
        Assert.Same(CloudEnvironment.USGov, CloudEnvironment.FromName("USGov"));
        Assert.Same(CloudEnvironment.USGovDoD, CloudEnvironment.FromName("USGovDoD"));
        Assert.Same(CloudEnvironment.China, CloudEnvironment.FromName("China"));
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("Azure")]
    public void FromName_ThrowsForUnknownName(string name)
    {
        Assert.Throws<ArgumentException>(() => CloudEnvironment.FromName(name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void FromName_ThrowsForEmptyOrWhitespace(string name)
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => CloudEnvironment.FromName(name));
        Assert.Contains("empty or whitespace", ex.Message);
    }

    [Fact]
    public void FromName_ThrowsForNull()
    {
        Assert.Throws<ArgumentNullException>(() => CloudEnvironment.FromName(null!));
    }

    [Fact]
    public void Constructor_TrimsTrailingSlashOnLoginEndpointAndTokenServiceUrl()
    {
        var env = new CloudEnvironment(
            loginEndpoint: "https://example.com/",
            loginTenant: "tenant",
            botScope: "scope",
            tokenServiceUrl: "https://token.example.com/",
            openIdMetadataUrl: "https://oidc.example.com",
            tokenIssuer: "issuer",
            graphScope: "graph");

        Assert.Equal("https://example.com", env.LoginEndpoint);
        Assert.Equal("https://token.example.com", env.TokenServiceUrl);
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

        var result = env.WithOverrides(tokenIssuer: "https://custom.issuer");

        Assert.NotSame(env, result);
        Assert.Equal("https://custom.issuer", result.TokenIssuer);
        Assert.Equal(env.LoginEndpoint, result.LoginEndpoint);
        Assert.Equal(env.BotScope, result.BotScope);
        Assert.Equal(env.TokenServiceUrl, result.TokenServiceUrl);
        Assert.Equal(env.OpenIdMetadataUrl, result.OpenIdMetadataUrl);
        Assert.Equal(env.GraphScope, result.GraphScope);
    }

    [Fact]
    public void WithOverrides_AllOverrides_ReplacesAllProperties()
    {
        var env = CloudEnvironment.Public;

        var result = env.WithOverrides(
            loginEndpoint: "https://a",
            loginTenant: "b",
            botScope: "c",
            tokenServiceUrl: "https://d",
            openIdMetadataUrl: "e",
            tokenIssuer: "f",
            graphScope: "g");

        Assert.Equal("https://a", result.LoginEndpoint);
        Assert.Equal("b", result.LoginTenant);
        Assert.Equal("c", result.BotScope);
        Assert.Equal("https://d", result.TokenServiceUrl);
        Assert.Equal("e", result.OpenIdMetadataUrl);
        Assert.Equal("f", result.TokenIssuer);
        Assert.Equal("g", result.GraphScope);
    }
}
