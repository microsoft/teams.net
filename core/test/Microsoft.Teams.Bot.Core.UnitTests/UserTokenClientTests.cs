// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;

namespace Microsoft.Teams.Bot.Core.UnitTests;

public class UserTokenClientTests
{
    [Fact]
    public void UserTokenClient_SetsUserAgentHeader()
    {
        // Arrange
        HttpClient httpClient = new();
        IConfiguration configuration = new ConfigurationBuilder().Build();

        // Act
        UserTokenClient userTokenClient = new(httpClient, configuration, null!);

        // Assert
        Assert.True(userTokenClient.DefaultCustomHeaders.ContainsKey("User-Agent"));
        var userAgent = userTokenClient.DefaultCustomHeaders["User-Agent"];
        Assert.NotNull(userAgent);
        Assert.Contains("Microsoft.Teams.Bot.Core", userAgent);
        Assert.Contains("/", userAgent); // Should have format Name/Version
        Assert.Matches(@"^[\w\.]+/[\w\.\-\+]+$", userAgent); // Validates RFC 7231 format
    }
}
