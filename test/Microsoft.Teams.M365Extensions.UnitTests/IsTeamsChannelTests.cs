// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Agents.Core.Models;

namespace Microsoft.Teams.M365Extensions.UnitTests;

public class IsTeamsChannelTests
{
    [Theory]
    [InlineData("msteams")]
    [InlineData("MSTEAMS")]
    [InlineData("msteams:COPILOT")]
    [InlineData("msteams:anything-else")]
    public void IsTeamsChannel_ReturnsTrue_ForTeamsChannels(string channelId)
    {
        var activity = new Activity { ChannelId = channelId };

        Assert.True(TeamsSdkMiddleware.IsTeamsChannel(activity));
    }

    [Theory]
    [InlineData("webchat")]
    [InlineData("directline")]
    [InlineData("emulator")]
    [InlineData("email")]
    [InlineData("msteamsx")]
    public void IsTeamsChannel_ReturnsFalse_ForNonTeamsChannels(string channelId)
    {
        var activity = new Activity { ChannelId = channelId };

        Assert.False(TeamsSdkMiddleware.IsTeamsChannel(activity));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void IsTeamsChannel_ReturnsFalse_ForEmptyOrWhitespace(string channelId)
    {
        var activity = new Activity { ChannelId = channelId };

        Assert.False(TeamsSdkMiddleware.IsTeamsChannel(activity));
    }

    [Fact]
    public void IsTeamsChannel_Throws_ForNullActivity()
    {
        Assert.Throws<ArgumentNullException>(() => TeamsSdkMiddleware.IsTeamsChannel(null!));
    }
}
