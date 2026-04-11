// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Schema;
using Microsoft.Teams.Bot.Compat;
using Moq;

namespace Microsoft.Teams.Bot.Compat.UnitTests;

/// <summary>
/// Verifies that methods accepting IActivity in CompatTeamsInfo throw a descriptive
/// ArgumentException instead of an InvalidCastException when the activity is not an
/// instance of the Bot Framework Activity class (A-016).
/// </summary>
public class CompatTeamsInfoCastTests
{
    [Fact]
    public async Task SendMessageToListOfUsersAsync_WithNonActivityIActivity_ThrowsDescriptiveArgumentException()
    {
        // Arrange – a mock ITurnContext backed by a real Activity (required by the helper)
        TestAdapter adapter = new();
        Activity seed = new() { Type = ActivityTypes.Message, ServiceUrl = "https://example.com/" };
        ITurnContext ctx = new TurnContext(adapter, seed);

        // Create a mock IActivity that is NOT an Activity instance
        Mock<IActivity> mockActivity = new();

        // Act & Assert – must throw ArgumentException, not InvalidCastException
        ArgumentException ex = await Assert.ThrowsAsync<ArgumentException>(
            () => CompatTeamsInfo.SendMessageToListOfUsersAsync(
                ctx,
                mockActivity.Object,
                [new Microsoft.Bot.Schema.Teams.TeamMember { Id = "user-1" }],
                "tenant-1",
                CancellationToken.None));

        Assert.Equal("activity", ex.ParamName);
        Assert.Contains("Activity", ex.Message);
    }

    [Fact]
    public async Task SendMessageToListOfChannelsAsync_WithNonActivityIActivity_ThrowsArgumentException()
    {
        TestAdapter adapter = new();
        Activity seed = new() { Type = ActivityTypes.Message, ServiceUrl = "https://example.com/" };
        ITurnContext ctx = new TurnContext(adapter, seed);

        Mock<IActivity> mockActivity = new();

        ArgumentException ex = await Assert.ThrowsAsync<ArgumentException>(
            () => CompatTeamsInfo.SendMessageToListOfChannelsAsync(
                ctx,
                mockActivity.Object,
                [new Microsoft.Bot.Schema.Teams.TeamMember { Id = "channel-1" }],
                "tenant-1",
                CancellationToken.None));

        Assert.Equal("activity", ex.ParamName);
    }
}
