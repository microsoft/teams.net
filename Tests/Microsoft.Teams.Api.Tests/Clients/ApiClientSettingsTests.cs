using Microsoft.Teams.Api.Clients;

namespace Microsoft.Teams.Api.Tests.Clients;

public class ApiClientSettingsTests
{
    [Fact]
    public void ApiClientSettings_Default_UsesDefaultOAuthUrl()
    {
        var settings = new ApiClientSettings();

        Assert.Equal("https://token.botframework.com", settings.OAuthUrl);
    }

    [Fact]
    public void ApiClientSettings_WithCustomOAuthUrl()
    {
        var settings = new ApiClientSettings("https://europe.token.botframework.com");

        Assert.Equal("https://europe.token.botframework.com", settings.OAuthUrl);
    }

    [Fact]
    public void ApiClientSettings_Merge_WithNullSettings_ReturnsDefault()
    {
        var merged = ApiClientSettings.Merge(null);

        Assert.Equal("https://token.botframework.com", merged.OAuthUrl);
    }

    [Fact]
    public void ApiClientSettings_Merge_WithCustomSettings_UsesCustomUrl()
    {
        var customSettings = new ApiClientSettings("https://europe.token.botframework.com");
        var merged = ApiClientSettings.Merge(customSettings);

        Assert.Equal("https://europe.token.botframework.com", merged.OAuthUrl);
    }

    [Fact]
    public void ApiClientSettings_Merge_WithEnvironmentVariable_UsesEnvironmentUrl()
    {
        // Set environment variable
        Environment.SetEnvironmentVariable("OAUTH_URL", "https://asia.token.botframework.com");

        try
        {
            var customSettings = new ApiClientSettings("https://europe.token.botframework.com");
            var merged = ApiClientSettings.Merge(customSettings);

            Assert.Equal("https://asia.token.botframework.com", merged.OAuthUrl);
        }
        finally
        {
            // Clean up
            Environment.SetEnvironmentVariable("OAUTH_URL", null);
        }
    }

    [Fact]
    public void ApiClientSettings_Default_Property()
    {
        var defaultSettings = ApiClientSettings.Default;

        Assert.NotNull(defaultSettings);
        Assert.Equal("https://token.botframework.com", defaultSettings.OAuthUrl);
    }
}
