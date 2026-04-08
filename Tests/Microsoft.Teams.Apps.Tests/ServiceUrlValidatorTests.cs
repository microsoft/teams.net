namespace Microsoft.Teams.Apps.Tests;

public class ServiceUrlValidatorTests
{
    // --- Default allowed domains ---

    [Theory]
    [InlineData("https://smba.trafficmanager.net/teams/")]
    [InlineData("https://smba.trafficmanager.net/amer/")]
    [InlineData("https://webchat.botframework.com")]
    [InlineData("https://directline.botframework.com")]
    [InlineData("https://smba.infra.gcc.teams.microsoft.com/")]
    [InlineData("https://smba.infra.gov.teams.microsoft.us/gcch/")]
    [InlineData("https://directline.botframework.azure.us")]
    [InlineData("https://frontend.botapi.msg.infra.teams.microsoftonline.cn")]
    [InlineData("https://directline.botframework.azure.cn")]
    public void IsAllowed_AcceptsKnownBotFrameworkDomains(string serviceUrl)
    {
        Assert.True(ServiceUrlValidator.IsAllowed(serviceUrl));
    }

    // --- Localhost ---

    [Theory]
    [InlineData("http://localhost:3978")]
    [InlineData("https://localhost:443")]
    [InlineData("http://127.0.0.1:3978")]
    public void IsAllowed_AcceptsLocalhost(string serviceUrl)
    {
        Assert.True(ServiceUrlValidator.IsAllowed(serviceUrl));
    }

    // --- Rejected domains ---

    [Theory]
    [InlineData("https://evil.com")]
    [InlineData("https://botframework.com.evil.com")]
    [InlineData("https://attacker.net/api")]
    public void IsAllowed_RejectsUnknownDomains(string serviceUrl)
    {
        Assert.False(ServiceUrlValidator.IsAllowed(serviceUrl));
    }

    // --- Empty / null ---

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void IsAllowed_AcceptsEmptyOrNull(string? serviceUrl)
    {
        Assert.True(ServiceUrlValidator.IsAllowed(serviceUrl!));
    }

    // --- Invalid URLs ---

    [Fact]
    public void IsAllowed_RejectsInvalidUrl()
    {
        Assert.False(ServiceUrlValidator.IsAllowed("not-a-url"));
    }

    // --- Additional domains ---

    [Fact]
    public void IsAllowed_AcceptsAdditionalDomains()
    {
        var additional = new[] { ".custom-channel.com" };
        Assert.True(ServiceUrlValidator.IsAllowed("https://api.custom-channel.com", additional));
    }

    [Fact]
    public void IsAllowed_RejectsWhenNotInAdditionalDomains()
    {
        var additional = new[] { ".custom-channel.com" };
        Assert.False(ServiceUrlValidator.IsAllowed("https://evil.com", additional));
    }
}
