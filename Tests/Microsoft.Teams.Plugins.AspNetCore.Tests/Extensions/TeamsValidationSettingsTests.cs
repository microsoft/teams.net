// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

namespace Microsoft.Teams.Plugins.AspNetCore.Tests.Extensions;

public class TeamsValidationSettingsTests
{
    [Fact]
    public void DefaultConstructor_UsesPublicCloud()
    {
        var settings = new TeamsValidationSettings();

        Assert.Equal("https://login.botframework.com/v1/.well-known/openidconfiguration", settings.OpenIdMetadataUrl);
        Assert.Equal("https://login.microsoftonline.com", settings.LoginEndpoint);
        Assert.Contains("https://api.botframework.com", settings.Issuers);
    }

    [Fact]
    public void USGovCloud_HasCorrectSettings()
    {
        var settings = new TeamsValidationSettings(CloudEnvironment.USGov);

        Assert.Equal("https://login.botframework.azure.us/v1/.well-known/openidconfiguration", settings.OpenIdMetadataUrl);
        Assert.Equal("https://login.microsoftonline.us", settings.LoginEndpoint);
        Assert.Contains("https://api.botframework.us", settings.Issuers);
    }

    [Fact]
    public void ChinaCloud_HasCorrectSettings()
    {
        var settings = new TeamsValidationSettings(CloudEnvironment.China);

        Assert.Equal("https://login.botframework.azure.cn/v1/.well-known/openidconfiguration", settings.OpenIdMetadataUrl);
        Assert.Equal("https://login.partner.microsoftonline.cn", settings.LoginEndpoint);
        Assert.Contains("https://api.botframework.azure.cn", settings.Issuers);
    }

    [Fact]
    public void AllClouds_IncludeEmulatorIssuers()
    {
        var clouds = new[] { CloudEnvironment.Public, CloudEnvironment.USGov, CloudEnvironment.USGovDoD, CloudEnvironment.China };

        foreach (var cloud in clouds)
        {
            var settings = new TeamsValidationSettings(cloud);

            // Emulator issuers should always be present
            Assert.Contains(settings.Issuers, i => i.Contains("d6d49420-f39b-4df7-a1dc-d59a935871db"));
            Assert.Contains(settings.Issuers, i => i.Contains("f8cdef31-a31e-4b4a-93e4-5f571e91255a"));
        }
    }

    [Fact]
    public void GetTenantSpecificOpenIdMetadataUrl_UsesCloudLoginEndpoint()
    {
        var settings = new TeamsValidationSettings(CloudEnvironment.USGov);

        var url = settings.GetTenantSpecificOpenIdMetadataUrl("my-tenant");

        Assert.Equal("https://login.microsoftonline.us/my-tenant/v2.0/.well-known/openid-configuration", url);
    }

    [Fact]
    public void GetTenantSpecificOpenIdMetadataUrl_DefaultsToCommon()
    {
        var settings = new TeamsValidationSettings(CloudEnvironment.China);

        var url = settings.GetTenantSpecificOpenIdMetadataUrl(null);

        Assert.Equal("https://login.partner.microsoftonline.cn/common/v2.0/.well-known/openid-configuration", url);
    }

    [Fact]
    public void GetValidIssuersForTenant_UsesCloudLoginEndpoint()
    {
        var settings = new TeamsValidationSettings(CloudEnvironment.USGov);

        var issuers = settings.GetValidIssuersForTenant("my-tenant").ToList();

        Assert.Single(issuers);
        Assert.Equal("https://login.microsoftonline.us/my-tenant/", issuers[0]);
    }

    [Fact]
    public void GetValidIssuersForTenant_ReturnsEmptyForNullTenant()
    {
        var settings = new TeamsValidationSettings(CloudEnvironment.USGov);

        var issuers = settings.GetValidIssuersForTenant(null).ToList();

        Assert.Empty(issuers);
    }

    [Fact]
    public void AddDefaultAudiences_AddsClientIdAndApiPrefix()
    {
        var settings = new TeamsValidationSettings(CloudEnvironment.USGov);

        settings.AddDefaultAudiences("my-client-id");

        Assert.Contains("my-client-id", settings.Audiences);
        Assert.Contains("api://my-client-id", settings.Audiences);
    }
}
