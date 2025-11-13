using Microsoft.Teams.Api.Clients;

namespace Microsoft.Teams.Api.Tests.Clients;

public class ApiClientOptionsTests
{
    [Fact]
    public void ApiClientOptions_Default_UsesDefaultOAuthUrl()
    {
        var settings = new ApiClientOptions();

        Assert.Equal("https://token.botframework.com", settings.OAuthUrl);
    }

    [Fact]
    public void ApiClientOptions_WithCustomOAuthUrl()
    {
        var settings = new ApiClientOptions("https://europe.token.botframework.com");

        Assert.Equal("https://europe.token.botframework.com", settings.OAuthUrl);
    }

    [Fact]
    public void ApiClientOptions_Merge_WithNullSettings_ReturnsDefault()
    {
        var merged = ApiClientOptions.Merge(null);

        Assert.Equal("https://token.botframework.com", merged.OAuthUrl);
    }

    [Fact]
    public void ApiClientOptions_Merge_WithCustomSettings_UsesCustomUrl()
    {
        var customSettings = new ApiClientOptions("https://europe.token.botframework.com");
        var merged = ApiClientOptions.Merge(customSettings);

        Assert.Equal("https://europe.token.botframework.com", merged.OAuthUrl);
    }

    [Fact]
    public void ApiClientOptions_Merge_WithEnvironmentVariable_UsesEnvironmentUrl()
    {
        // Set environment variable
        Environment.SetEnvironmentVariable("OAUTH_URL", "https://asia.token.botframework.com");

        try
        {
            var customSettings = new ApiClientOptions("https://europe.token.botframework.com");
            var merged = ApiClientOptions.Merge(customSettings);

            Assert.Equal("https://asia.token.botframework.com", merged.OAuthUrl);
        }
        finally
        {
            // Clean up
            Environment.SetEnvironmentVariable("OAUTH_URL", null);
        }
    }

    [Fact]
    public void ApiClientOptions_Default_Property()
    {
        var defaultSettings = ApiClientOptions.Default;

        Assert.NotNull(defaultSettings);
        Assert.Equal("https://token.botframework.com", defaultSettings.OAuthUrl);
    }
}
