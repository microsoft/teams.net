// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Teams.Bot.Core;

namespace Microsoft.Teams.Bot.Compat.UnitTests;

public class CompatUserTokenClientTests
{
    [Fact]
    public void CompatUserTokenClient_AppendsUserAgentToWrappedClient()
    {
        // Arrange
        HttpClient httpClient = new();
        IConfiguration configuration = new ConfigurationBuilder().Build();
        UserTokenClient userTokenClient = new(httpClient, configuration, null!);
        var originalUserAgent = userTokenClient.DefaultCustomHeaders["User-Agent"];

        // Act
        CompatUserTokenClient compatUserTokenClient = new(userTokenClient);

        // Assert
        var modifiedUserAgent = userTokenClient.DefaultCustomHeaders["User-Agent"];
        Assert.NotEqual(originalUserAgent, modifiedUserAgent);

        // Should have Compat prepended
        Assert.StartsWith("Microsoft.Teams.Bot.Compat/", modifiedUserAgent);

        // Should contain original Core user agent
        Assert.Contains("Microsoft.Teams.Bot.Core/", modifiedUserAgent);

        // Should have space-separated format
        Assert.Contains(" ", modifiedUserAgent);

        // Validate it matches the pattern: Compat/version Core/version
        Assert.Matches(@"^Microsoft\.Teams\.Bot\.Compat/[\w\.\-\+]+ Microsoft\.Teams\.Bot\.Core/[\w\.\-\+]+$", modifiedUserAgent);
    }
}
