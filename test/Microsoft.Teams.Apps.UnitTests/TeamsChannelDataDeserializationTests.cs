// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;

using Microsoft.Teams.Apps.Schema;

namespace Microsoft.Teams.Apps.UnitTests;

/// <summary>
/// The Teams service can send nested channel data objects as empty objects. These are inbound-only
/// models, so a missing field must not fail deserialization of the whole payload.
/// See https://github.com/microsoft/teams.py/issues/563 for the equivalent break in the Python SDK.
/// </summary>
public class TeamsChannelDataDeserializationTests
{
    [Theory]
    [InlineData("{\"app\":{}}")]
    [InlineData("{\"channel\":{}}")]
    [InlineData("{\"team\":{}}")]
    [InlineData("{\"tenant\":{}}")]
    [InlineData("{\"settings\":{}}")]
    public void Deserialize_EmptyNestedObject_DoesNotThrow(string json)
    {
        TeamsChannelData? channelData = JsonSerializer.Deserialize<TeamsChannelData>(json);

        Assert.NotNull(channelData);
    }

    [Fact]
    public void Deserialize_EmptySettings_LeavesSelectedChannelNull()
    {
        TeamsChannelData? channelData = JsonSerializer.Deserialize<TeamsChannelData>("{\"settings\":{}}");

        Assert.NotNull(channelData);
        Assert.NotNull(channelData.Settings);
        Assert.Null(channelData.Settings.SelectedChannel);
    }

    [Fact]
    public void Deserialize_PopulatedChannelData_PreservesValues()
    {
        const string json = """
        {
            "app": { "id": "app-id", "version": "1.2.3" },
            "channel": { "id": "channel-id" },
            "team": { "id": "team-id" },
            "tenant": { "id": "tenant-id" },
            "settings": { "selectedChannel": { "id": "selected-channel-id" } }
        }
        """;

        TeamsChannelData? channelData = JsonSerializer.Deserialize<TeamsChannelData>(json);

        Assert.NotNull(channelData);
        Assert.Equal("app-id", channelData.App?.Id);
        Assert.Equal("1.2.3", channelData.App?.Version);
        Assert.Equal("channel-id", channelData.Channel?.Id);
        Assert.Equal("team-id", channelData.Team?.Id);
        Assert.Equal("tenant-id", channelData.Tenant?.Id);
        Assert.Equal("selected-channel-id", channelData.Settings?.SelectedChannel?.Id);
    }
}
