// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
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

        Assert.Throws<SecurityTokenInvalidIssuerException>(() =>
            JwtExtensions.ValidateTeamsIssuer(
                publicIssuer, token, Tenant, "https://login.microsoftonline.us/", "https://api.botframework.com"));
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
    public void AddBotAuthentication_ManualOverload_DoesNotThrow_WhenNoIConfigurationRegistered()
    {
        // Regression: AddBotAuthentication(clientId, tenantId) manual overload should remain usable
        // even when no IConfiguration is registered (e.g. plain ServiceCollection scenarios).
        ServiceCollection services = new();
        services.AddLogging();

        Exception? caught = Record.Exception(() => services.AddBotAuthentication(ClientId, Tenant));

        Assert.Null(caught);
    }
}
