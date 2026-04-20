using Microsoft.Teams.Api.Auth;

namespace Microsoft.Teams.Apps.Tests;

public class ServiceUrlValidatorTests
{
    // --- Public cloud ---

    [Theory]
    [InlineData("https://smba.trafficmanager.net/teams/")]
    [InlineData("https://smba.trafficmanager.net/amer/")]
    [InlineData("https://smba.onyx.prod.teams.trafficmanager.net")]
    public void IsAllowed_AcceptsPublicCloudDomains(string serviceUrl)
    {
        Assert.True(ServiceUrlValidator.IsAllowed(serviceUrl, CloudEnvironment.Public));
    }

    // --- Government clouds ---

    [Fact]
    public void IsAllowed_AcceptsUSGovDomain()
    {
        Assert.True(ServiceUrlValidator.IsAllowed("https://smba.infra.gov.teams.microsoft.us/gcch/", CloudEnvironment.USGov));
    }

    [Fact]
    public void IsAllowed_AcceptsDoDDomain()
    {
        Assert.True(ServiceUrlValidator.IsAllowed("https://smba.infra.dod.teams.microsoft.us/", CloudEnvironment.USGovDoD));
    }

    [Fact]
    public void IsAllowed_AcceptsChinaDomain()
    {
        Assert.True(ServiceUrlValidator.IsAllowed("https://frontend.botapi.msg.infra.teams.microsoftonline.cn", CloudEnvironment.China));
    }

    // --- Cross-cloud rejection ---

    [Fact]
    public void IsAllowed_RejectsGovDomainWithPublicCloud()
    {
        Assert.False(ServiceUrlValidator.IsAllowed("https://smba.infra.gov.teams.microsoft.us/", CloudEnvironment.Public));
    }

    // --- Localhost ---

    [Theory]
    [InlineData("http://localhost:3978")]
    [InlineData("https://localhost:443")]
    [InlineData("http://127.0.0.1:3978")]
    public void IsAllowed_AcceptsLocalhost(string serviceUrl)
    {
        Assert.True(ServiceUrlValidator.IsAllowed(serviceUrl, CloudEnvironment.Public));
    }

    // --- Rejected domains ---

    [Theory]
    [InlineData("https://evil.com")]
    [InlineData("https://botframework.com.evil.com")]
    [InlineData("https://attacker.net/api")]
    [InlineData("https://attacker.trafficmanager.net")]
    public void IsAllowed_RejectsUnknownDomains(string serviceUrl)
    {
        Assert.False(ServiceUrlValidator.IsAllowed(serviceUrl, CloudEnvironment.Public));
    }

    // --- Empty / null ---

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void IsAllowed_AcceptsEmptyOrNull(string? serviceUrl)
    {
        Assert.True(ServiceUrlValidator.IsAllowed(serviceUrl!, CloudEnvironment.Public));
    }

    // --- Invalid URLs ---

    [Fact]
    public void IsAllowed_RejectsInvalidUrl()
    {
        Assert.False(ServiceUrlValidator.IsAllowed("not-a-url", CloudEnvironment.Public));
    }

    // --- Additional domains ---

    [Fact]
    public void IsAllowed_AcceptsAdditionalDomains()
    {
        var additional = new[] { "api.custom-channel.com" };
        Assert.True(ServiceUrlValidator.IsAllowed("https://api.custom-channel.com", CloudEnvironment.Public, additional));
    }

    [Fact]
    public void IsAllowed_RejectsWhenNotInAdditionalDomains()
    {
        var additional = new[] { "api.custom-channel.com" };
        Assert.False(ServiceUrlValidator.IsAllowed("https://evil.com", CloudEnvironment.Public, additional));
    }

    // --- Wildcard ---

    [Fact]
    public void IsAllowed_AcceptsAnyDomainWithWildcard()
    {
        var additional = new[] { "*" };
        Assert.True(ServiceUrlValidator.IsAllowed("https://anything.example.com", CloudEnvironment.Public, additional));
    }

    // --- botframework.com not in default ---

    [Theory]
    [InlineData("https://webchat.botframework.com")]
    [InlineData("https://directline.botframework.com")]
    public void IsAllowed_RejectsBotframeworkByDefault(string serviceUrl)
    {
        Assert.False(ServiceUrlValidator.IsAllowed(serviceUrl, CloudEnvironment.Public));
    }
}
