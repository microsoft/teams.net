using Microsoft.Teams.Api.Auth;

namespace Microsoft.Teams.Api.Tests.Auth;

public class ClientCredentialsTests
{
    [Fact]
    public void Instance_DefaultsToPublicCloud()
    {
        var credentials = new ClientCredentials("client-id", "client-secret");

        Assert.Equal("https://login.microsoftonline.com", credentials.Instance);
    }

    [Fact]
    public void Instance_CanBeOverridden()
    {
        var credentials = new ClientCredentials("client-id", "client-secret")
        {
            Instance = "https://login.microsoftonline.us"
        };

        Assert.Equal("https://login.microsoftonline.us", credentials.Instance);
    }

    [Fact]
    public void Constructor_WithTenantId_SetsProperties()
    {
        var credentials = new ClientCredentials("client-id", "client-secret", "tenant-id");

        Assert.Equal("client-id", credentials.ClientId);
        Assert.Equal("client-secret", credentials.ClientSecret);
        Assert.Equal("tenant-id", credentials.TenantId);
    }
}
