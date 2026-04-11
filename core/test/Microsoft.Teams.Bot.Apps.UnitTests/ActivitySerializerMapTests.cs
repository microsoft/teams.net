// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Teams.Bot.Apps.Handlers;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.UnitTests;

/// <summary>
/// Verifies that ActivitySerializerMap is symmetric with ActivityDeserializerMap (SER-01)
/// and that a full serialization round-trip preserves subtype-specific fields.
/// </summary>
public class ActivitySerializerMapTests
{
    [Fact]
    public void SerializerMap_ContainsEntryForEveryDeserializerType()
    {
        // Every type produced by the deserializer must have a matching serializer so that
        // subtype-specific fields (e.g. ReactionsAdded, MembersAdded) are not silently dropped.
        foreach (string activityType in TeamsActivityType.ActivityDeserializerMap.Keys)
        {
            CoreActivity core = new(activityType);
            TeamsActivity deserialized = TeamsActivity.FromActivity(core);
            Type concreteType = deserialized.GetType();

            Assert.True(
                TeamsActivityType.ActivitySerializerMap.ContainsKey(concreteType),
                $"ActivitySerializerMap is missing an entry for '{concreteType.Name}' " +
                $"(activity type '{activityType}'). Add it to keep the maps symmetric.");
        }
    }

    [Fact]
    public void SerializerMap_Count_EqualsDeserializerMap_Count_PlusStreamingActivity()
    {
        // Serializer has all deserializer types + StreamingActivity (which has no incoming path).
        // So: serializer.Count == deserializer.Count + 1
        int deserializerCount = TeamsActivityType.ActivityDeserializerMap.Count;
        int serializerCount = TeamsActivityType.ActivitySerializerMap.Count;
        Assert.Equal(deserializerCount + 1, serializerCount);
    }

    [Theory]
    [InlineData(TeamsActivityType.MessageReaction, "reactionsAdded", """[{"type":"like"}]""")]
    [InlineData(TeamsActivityType.ConversationUpdate, "membersAdded", """[{"id":"user1","name":"Alice"}]""")]
    public void RoundTrip_PreservesSubtypeFields(string activityType, string subtypeProperty, string subtypeValue)
    {
        // Arrange – build a CoreActivity that carries a subtype-specific field
        CoreActivity core = new(activityType);
        core.Properties[subtypeProperty] = JsonSerializer.Deserialize<JsonElement>(subtypeValue);

        // Act – deserialize into the specialized type, then serialize back
        TeamsActivity teamsActivity = TeamsActivity.FromActivity(core);
        string json = teamsActivity.ToJson();

        // Assert – the subtype-specific field survived the round-trip
        Assert.Contains(subtypeProperty, json);
    }

    [Fact]
    public void MessageReactionActivity_RoundTrip_PreservesReactions()
    {
        // Arrange
        CoreActivity core = new(TeamsActivityType.MessageReaction);
        core.Properties["reactionsAdded"] = JsonSerializer.Deserialize<JsonElement>("""[{"type":"like"},{"type":"heart"}]""");
        core.Properties["replyToId"] = JsonSerializer.Deserialize<JsonElement>("\"msg-001\"");

        // Act
        TeamsActivity teamsActivity = TeamsActivity.FromActivity(core);
        Assert.IsType<MessageReactionActivity>(teamsActivity);

        string json = teamsActivity.ToJson();

        // Assert – both reaction-specific fields are present
        Assert.Contains("reactionsAdded", json);
        Assert.Contains("like", json);
        Assert.Contains("replyToId", json);
        Assert.Contains("msg-001", json);
    }
}
