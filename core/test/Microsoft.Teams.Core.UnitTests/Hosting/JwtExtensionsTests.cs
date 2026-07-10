// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Teams.Core.Hosting;

namespace Microsoft.Teams.Core.UnitTests.Hosting;

public class JwtExtensionsTests
{
    private const string Tenant = "00000000-0000-0000-0000-000000000001";
    private const string ClientId = "11111111-1111-1111-1111-111111111111";

    private static SecurityToken FakeJsonWebToken(string tenantId)
    {
        // Minimal JWT-shaped string with a tid claim for token-claim extraction.
        JsonWebTokenHandler handler = new();
        SecurityTokenDescriptor descriptor = new()
        {
            Issuer = "unused-by-test",
            Claims = new Dictionary<string, object> { ["tid"] = tenantId },
        };
        return new JsonWebToken(handler.CreateToken(descriptor));
    }

    [Fact]
    public void ValidateTeamsIssuer_AcceptsBotFrameworkIssuer()
    {
        SecurityToken token = FakeJsonWebToken(Tenant);

        string result = JwtExtensions.ValidateTeamsIssuer(
            "https://api.botframework.com", token, Tenant, "https://login.microsoftonline.com/", "https://api.botframework.com");

        Assert.Equal("https://api.botframework.com", result);
    }

    [Theory]
    [InlineData("https://api.botframework.us")]
    [InlineData("https://api.botframework.azure.cn")]
    public void ValidateTeamsIssuer_AcceptsSovereignBotFrameworkIssuer_WhenConfigured(string sovereignBotIssuer)
    {
        SecurityToken token = FakeJsonWebToken(Tenant);

        string result = JwtExtensions.ValidateTeamsIssuer(
            sovereignBotIssuer, token, Tenant, "https://login.microsoftonline.us/", sovereignBotIssuer);

        Assert.Equal(sovereignBotIssuer, result);
    }

    [Fact]
    public void ValidateTeamsIssuer_RejectsPublicBotIssuer_WhenSovereignBotIssuerConfigured()
    {
        SecurityToken token = FakeJsonWebToken(Tenant);

        Assert.Throws<SecurityTokenInvalidIssuerException>(() =>
            JwtExtensions.ValidateTeamsIssuer(
                "https://api.botframework.com", token, Tenant,
                "https://login.microsoftonline.us/", "https://api.botframework.us"));
    }

    [Fact]
    public void ValidateTeamsIssuer_AcceptsPublicEntraV2Issuer()
    {
        SecurityToken token = FakeJsonWebToken(Tenant);
        string issuer = $"https://login.microsoftonline.com/{Tenant}/v2.0";

        string result = JwtExtensions.ValidateTeamsIssuer(
            issuer, token, Tenant, "https://login.microsoftonline.com/", "https://api.botframework.com");

        Assert.Equal(issuer, result);
    }

    [Fact]
    public void ValidateTeamsIssuer_AcceptsSovereignEntraIssuer_WhenInstanceConfigured()
    {
        SecurityToken token = FakeJsonWebToken(Tenant);
        string sovereignInstance = "https://login.microsoftonline.us/";
        string issuer = $"{sovereignInstance}{Tenant}/v2.0";

        string result = JwtExtensions.ValidateTeamsIssuer(
            issuer, token, Tenant, sovereignInstance, "https://api.botframework.com");

        Assert.Equal(issuer, result);
    }

    [Fact]
    public void ValidateTeamsIssuer_RejectsPublicEntraIssuer_WhenSovereignInstanceConfigured()
    {
        SecurityToken token = FakeJsonWebToken(Tenant);
        string publicIssuer = $"https://login.microsoftonline.com/{Tenant}/v2.0";

        SecurityTokenInvalidIssuerException ex = Assert.Throws<SecurityTokenInvalidIssuerException>(() =>
            JwtExtensions.ValidateTeamsIssuer(
                publicIssuer, token, Tenant, "https://login.microsoftonline.us/", "https://api.botframework.com"));

        Assert.Contains(publicIssuer, ex.Message, StringComparison.Ordinal);
        Assert.Contains(Tenant, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateTeamsIssuer_AcceptsStsWindowsNetV1Issuer()
    {
        SecurityToken token = FakeJsonWebToken(Tenant);
        string issuer = $"https://sts.windows.net/{Tenant}/";

        string result = JwtExtensions.ValidateTeamsIssuer(
            issuer, token, Tenant, "https://login.microsoftonline.com/", "https://api.botframework.com");

        Assert.Equal(issuer, result);
    }

    [Fact]
    public void ResolveSigningAuthority_RoutesPublicBotIssuer_ToConfiguredBotOidcUrl()
    {
        string authority = JwtExtensions.ResolveSigningAuthority(
            iss: "https://api.botframework.com",
            tid: Tenant,
            botTokenIssuer: "https://api.botframework.com",
            botOidcUrl: "https://login.botframework.com/v1/.well-known/openid-configuration",
            entraInstance: "https://login.microsoftonline.com/");

        Assert.Equal("https://login.botframework.com/v1/.well-known/openid-configuration", authority);
    }

    [Theory]
    [InlineData("https://api.botframework.us", "https://login.botframework.azure.us/v1/.well-known/openid-configuration")]
    [InlineData("https://api.botframework.azure.cn", "https://login.botframework.azure.cn/v1/.well-known/openid-configuration")]
    public void ResolveSigningAuthority_RoutesSovereignBotIssuer_ToConfiguredBotOidcUrl(string sovereignBotIssuer, string sovereignBotOidcUrl)
    {
        string authority = JwtExtensions.ResolveSigningAuthority(
            iss: sovereignBotIssuer,
            tid: Tenant,
            botTokenIssuer: sovereignBotIssuer,
            botOidcUrl: sovereignBotOidcUrl,
            entraInstance: "https://login.microsoftonline.us/");

        Assert.Equal(sovereignBotOidcUrl, authority);
    }

    [Fact]
    public void ResolveSigningAuthority_RoutesEntraIssuer_ToInstanceDerivedAuthority()
    {
        string authority = JwtExtensions.ResolveSigningAuthority(
            iss: $"https://login.microsoftonline.com/{Tenant}/v2.0",
            tid: Tenant,
            botTokenIssuer: "https://api.botframework.com",
            botOidcUrl: "https://login.botframework.com/v1/.well-known/openid-configuration",
            entraInstance: "https://login.microsoftonline.com/");

        Assert.Equal($"https://login.microsoftonline.com/{Tenant}/v2.0/.well-known/openid-configuration", authority);
    }

    [Fact]
    public void ResolveSigningAuthority_RoutesEntraIssuer_ToSovereignInstanceWhenConfigured()
    {
        string authority = JwtExtensions.ResolveSigningAuthority(
            iss: $"https://login.microsoftonline.us/{Tenant}/v2.0",
            tid: Tenant,
            botTokenIssuer: "https://api.botframework.us",
            botOidcUrl: "https://login.botframework.azure.us/v1/.well-known/openid-configuration",
            entraInstance: "https://login.microsoftonline.us/");

        Assert.Equal($"https://login.microsoftonline.us/{Tenant}/v2.0/.well-known/openid-configuration", authority);
    }

    [Fact]
    public void ResolveSigningAuthority_ReturnsEmpty_WhenIssuerNull()
    {
        string authority = JwtExtensions.ResolveSigningAuthority(
            iss: null,
            tid: Tenant,
            botTokenIssuer: "https://api.botframework.com",
            botOidcUrl: "https://login.botframework.com/v1/.well-known/openid-configuration",
            entraInstance: "https://login.microsoftonline.com/");

        Assert.Equal(string.Empty, authority);
    }

    [Fact]
    public void AddBotAuthentication_ManualOverload_DoesNotThrow_WhenNoIConfigurationRegistered()
    {
        // Regression: AddBotAuthentication(clientId, tenantId) manual overload should remain usable
        // even when no IConfiguration is registered (e.g. plain ServiceCollection scenarios).
        ServiceCollection services = new();
        services.AddLogging();

        Exception? caught = Record.Exception(() => services.AddBotAuthentication(ClientId, Tenant));

        Assert.Null(caught);
    }

    [Fact]
    public void AddBotAuthentication_ConfiguresExpectedInboundAudiences()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddBotAuthentication(ClientId, Tenant);

        using ServiceProvider provider = services.BuildServiceProvider();
        JwtBearerOptions options = provider
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(BotConfig.DefaultSectionName);

        Assert.Equal(
            [ClientId, $"api://{ClientId}", $"api://botid-{ClientId}"],
            options.TokenValidationParameters.ValidAudiences);
    }
}
