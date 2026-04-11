// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Apps.Schema.Entities;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.UnitTests;

public class UnknownEntityTypeTests
{
    [Fact]
    public void UnknownType_IsPreserved_NotDropped()
    {
        CoreActivity activity = CoreActivity.FromJsonString(
            "{\"type\":\"message\",\"entities\":[{\"type\":\"BotMessageMetadata\",\"someField\":\"v\"},{\"type\":\"clientInfo\",\"platform\":\"Web\"}]}");
        TeamsActivity teamsActivity = TeamsActivity.FromActivity(activity);
        Assert.NotNull(teamsActivity.Entities);
        Assert.Equal(2, teamsActivity.Entities.Count);
    }

    [Fact]
    public void UnknownType_DeserializesAsBaseEntity_WithProperties()
    {
        CoreActivity activity = CoreActivity.FromJsonString(
            "{\"type\":\"message\",\"entities\":[{\"type\":\"BotMessageMetadata\",\"someField\":\"v\"}]}");
        TeamsActivity teamsActivity = TeamsActivity.FromActivity(activity);
        Entity unknown = teamsActivity.Entities![0];
        Assert.IsType<Entity>(unknown);
        Assert.Equal("BotMessageMetadata", unknown.Type);
        Assert.True(unknown.Properties.ContainsKey("someField"));
    }
}
