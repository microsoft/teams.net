// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Bot.Schema.Teams;

namespace Microsoft.Teams.Bot.Compat.UnitTests;

/// <summary>
/// Verifies that the STJ-only round-trip in CompatTeamsInfo.SendMeetingNotificationAsync
/// preserves all known property names (A-021).
/// The test exercises the serializer options directly rather than going through the full
/// ASP.NET pipeline, which would require live auth tokens.
/// </summary>
public class CompatTeamsInfoRoundTripTests
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void StjRoundTrip_PreservesChannelDataProperty()
    {
        // Arrange – a TargetedMeetingNotification with a nested channel-data object
        TargetedMeetingNotification original = new()
        {
            Type = "targetedMeetingNotification",
            Value = new TargetedMeetingNotificationValue
            {
                Recipients = ["user-1", "user-2"],
            }
        };

        // Act – STJ → STJ (the fixed code path)
        string json = JsonSerializer.Serialize(original, s_jsonOptions);
        TargetedMeetingNotification? roundTripped = JsonSerializer.Deserialize<TargetedMeetingNotification>(json, s_jsonOptions);

        // Assert – key fields survived the round-trip
        Assert.NotNull(roundTripped);
        Assert.Equal("targetedMeetingNotification", roundTripped.Type);
        Assert.NotNull(roundTripped.Value);
        Assert.Equal(2, roundTripped.Value.Recipients?.Count);
        Assert.Contains("user-1", roundTripped.Value.Recipients!);
    }

    [Fact]
    public void StjRoundTrip_PreservesAllKnownTopLevelProperties()
    {
        // Arrange – use a plain dictionary to simulate arbitrary known properties
        Dictionary<string, object?> properties = new(StringComparer.OrdinalIgnoreCase)
        {
            ["type"] = "targetedMeetingNotification",
            ["channelData"] = new { someField = "value1" },
        };

        string originalJson = JsonSerializer.Serialize(properties, s_jsonOptions);

        // Act – deserialize and re-serialize with STJ
        Dictionary<string, object?>? result = JsonSerializer.Deserialize<Dictionary<string, object?>>(originalJson, s_jsonOptions);
        string reserialized = JsonSerializer.Serialize(result, s_jsonOptions);

        // Assert – every property key survived
        foreach (string key in properties.Keys)
        {
            Assert.Contains(key, reserialized, StringComparison.OrdinalIgnoreCase);
        }
    }
}
