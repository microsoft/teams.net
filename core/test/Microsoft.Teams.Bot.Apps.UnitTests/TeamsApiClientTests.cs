// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Bot.Apps.UnitTests;

public class TeamsApiClientTests
{
    [Fact]
    public void TeamsApiClient_SetsUserAgentHeader()
    {
        // Arrange
        HttpClient httpClient = new();

        // Act
        TeamsApiClient teamsApiClient = new(httpClient);

        // Assert
        Assert.True(teamsApiClient.DefaultCustomHeaders.ContainsKey("User-Agent"));
        var userAgent = teamsApiClient.DefaultCustomHeaders["User-Agent"];
        Assert.NotNull(userAgent);
        Assert.Contains("Microsoft.Teams.Bot.Apps", userAgent);
        Assert.Contains("/", userAgent); // Should have format Name/Version
        Assert.Matches(@"^[\w\.]+/[\w\.\-\+]+$", userAgent); // Validates RFC 7231 format
    }
}
