using Microsoft.Teams.Plugins.AspNetCore.Extensions;

namespace Microsoft.Teams.Plugins.AspNetCore.Tests.Extensions;

public class TeamsValidationSettingsTests
{
    [Fact]
    public void Instance_DefaultsToPublicCloud()
    {
        var settings = new TeamsValidationSettings();

        Assert.Equal("https://login.microsoftonline.com", settings.Instance);
    }

    [Fact]
    public void GetValidIssuersForTenant_UsesDefaultInstance()
    {
        var settings = new TeamsValidationSettings();
        var issuers = settings.GetValidIssuersForTenant("test-tenant").ToList();

        Assert.Single(issuers);
        Assert.Equal("https://login.microsoftonline.com/test-tenant/", issuers[0]);
    }

    [Fact]
    public void GetValidIssuersForTenant_UsesCustomInstance()
    {
        var settings = new TeamsValidationSettings
        {
            Instance = "https://login.microsoftonline.us"
        };
        var issuers = settings.GetValidIssuersForTenant("test-tenant").ToList();

        Assert.Single(issuers);
        Assert.Equal("https://login.microsoftonline.us/test-tenant/", issuers[0]);
    }

    [Fact]
    public void GetValidIssuersForTenant_HandlesTrailingSlashInInstance()
    {
        var settings = new TeamsValidationSettings
        {
            Instance = "https://login.microsoftonline.us/"
        };
        var issuers = settings.GetValidIssuersForTenant("test-tenant").ToList();

        Assert.Single(issuers);
        Assert.Equal("https://login.microsoftonline.us/test-tenant/", issuers[0]);
    }

    [Fact]
    public void GetTenantSpecificOpenIdMetadataUrl_UsesDefaultInstance()
    {
        var settings = new TeamsValidationSettings();
        var url = settings.GetTenantSpecificOpenIdMetadataUrl("test-tenant");

        Assert.Equal("https://login.microsoftonline.com/test-tenant/v2.0/.well-known/openid-configuration", url);
    }

    [Fact]
    public void GetTenantSpecificOpenIdMetadataUrl_UsesCustomInstance()
    {
        var settings = new TeamsValidationSettings
        {
            Instance = "https://login.microsoftonline.us"
        };
        var url = settings.GetTenantSpecificOpenIdMetadataUrl("test-tenant");

        Assert.Equal("https://login.microsoftonline.us/test-tenant/v2.0/.well-known/openid-configuration", url);
    }

    [Fact]
    public void GetTenantSpecificOpenIdMetadataUrl_UsesCommon_WhenTenantIdIsNull()
    {
        var settings = new TeamsValidationSettings();
        var url = settings.GetTenantSpecificOpenIdMetadataUrl(null);

        Assert.Equal("https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration", url);
    }

    [Fact]
    public void GetValidIssuersForTenant_ReturnsEmpty_WhenTenantIdIsNull()
    {
        var settings = new TeamsValidationSettings();
        var issuers = settings.GetValidIssuersForTenant(null).ToList();

        Assert.Empty(issuers);
    }
}
