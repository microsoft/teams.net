// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Teams.Bot.Core.Hosting;

namespace Microsoft.Teams.Bot.Core.UnitTests.Hosting;

public class JwtExtensionsTests
{
    private const string Tenant = "00000000-0000-0000-0000-000000000001";

    private static SecurityToken MakeToken(string? tid = null)
    {
        JwtSecurityTokenHandler handler = new();
        JwtSecurityToken jwt = new(
            claims: tid is null ? [] : [new Claim("tid", tid)]);
        return new JsonWebToken(handler.WriteToken(jwt));
    }

    [Fact]
    public void ValidateTeamsIssuer_AcceptsBotFrameworkIssuerForPublic()
    {
        string result = JwtExtensions.ValidateTeamsIssuer(
            "https://api.botframework.com",
            MakeToken(),
            configuredTenantId: "",
            CloudEnvironment.Public);

        Assert.Equal("https://api.botframework.com", result);
    }

    [Fact]
    public void ValidateTeamsIssuer_AcceptsBotFrameworkIssuerForUSGov()
    {
        string result = JwtExtensions.ValidateTeamsIssuer(
            "https://api.botframework.us",
            MakeToken(),
            configuredTenantId: "",
            CloudEnvironment.USGov);

        Assert.Equal("https://api.botframework.us", result);
    }

    [Fact]
    public void ValidateTeamsIssuer_RejectsPublicBotIssuerWhenCloudIsUSGov()
    {
        Assert.Throws<SecurityTokenInvalidIssuerException>(() =>
            JwtExtensions.ValidateTeamsIssuer(
                "https://api.botframework.com",
                MakeToken(),
                configuredTenantId: "",
                CloudEnvironment.USGov));
    }

    [Fact]
    public void ValidateTeamsIssuer_AcceptsEntraV2IssuerForUSGov()
    {
        string result = JwtExtensions.ValidateTeamsIssuer(
            $"https://login.microsoftonline.us/{Tenant}/v2.0",
            MakeToken(tid: Tenant),
            configuredTenantId: Tenant,
            CloudEnvironment.USGov);

        Assert.Equal($"https://login.microsoftonline.us/{Tenant}/v2.0", result);
    }

    [Fact]
    public void ValidateTeamsIssuer_RejectsPublicEntraV2IssuerWhenCloudIsUSGov()
    {
        Assert.Throws<SecurityTokenInvalidIssuerException>(() =>
            JwtExtensions.ValidateTeamsIssuer(
                $"https://login.microsoftonline.com/{Tenant}/v2.0",
                MakeToken(tid: Tenant),
                configuredTenantId: Tenant,
                CloudEnvironment.USGov));
    }

    [Fact]
    public void ValidateTeamsIssuer_AcceptsEntraV2IssuerForChina()
    {
        string result = JwtExtensions.ValidateTeamsIssuer(
            $"https://login.partner.microsoftonline.cn/{Tenant}/v2.0",
            MakeToken(tid: Tenant),
            configuredTenantId: Tenant,
            CloudEnvironment.China);

        Assert.Equal($"https://login.partner.microsoftonline.cn/{Tenant}/v2.0", result);
    }

    [Fact]
    public void ValidateTeamsIssuer_UsesTokenTidWhenConfiguredTenantEmpty()
    {
        string result = JwtExtensions.ValidateTeamsIssuer(
            $"https://login.microsoftonline.com/{Tenant}/v2.0",
            MakeToken(tid: Tenant),
            configuredTenantId: "",
            CloudEnvironment.Public);

        Assert.Equal($"https://login.microsoftonline.com/{Tenant}/v2.0", result);
    }

    [Fact]
    public void ValidateTeamsIssuer_AcceptsStsV1IssuerForPublic()
    {
        // v1.0 STS issuer: kept as hardcoded sts.windows.net. This is a known limitation
        // for sovereign clouds that use a different STS (e.g. China: sts.chinacloudapi.cn).
        string result = JwtExtensions.ValidateTeamsIssuer(
            $"https://sts.windows.net/{Tenant}/",
            MakeToken(tid: Tenant),
            configuredTenantId: Tenant,
            CloudEnvironment.Public);

        Assert.Equal($"https://sts.windows.net/{Tenant}/", result);
    }

    [Fact]
    public void ValidateTeamsIssuer_RejectsUnknownIssuer()
    {
        Assert.Throws<SecurityTokenInvalidIssuerException>(() =>
            JwtExtensions.ValidateTeamsIssuer(
                "https://evil.example.com",
                MakeToken(tid: Tenant),
                configuredTenantId: Tenant,
                CloudEnvironment.Public));
    }
}
