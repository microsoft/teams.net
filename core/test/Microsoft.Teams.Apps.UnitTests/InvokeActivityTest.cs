// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.UnitTests;

public class InvokeActivityTest
{
    [Fact]
    public void DefaultCtor()
    {
        InvokeActivity ia = new();
        Assert.NotNull(ia);
        Assert.Equal(TeamsActivityType.Invoke, ia.Type);
        Assert.Null(ia.Name);
        Assert.Null(ia.Value);
        // Assert.Null(ia.Conversation);
    }

    [Fact]
    public void FromCoreActivityWithValue()
    {
        // Build from JSON so that conversation lands in Properties as a JsonElement
        CoreActivity coreActivity = CoreActivity.FromJsonString("""
            {
                "type": "invoke",
                "value": { "key": "value" },
                "conversation": { "id": "convId" },
                "name": "testName"
            }
            """);
        InvokeActivity ia = InvokeActivity.FromActivity(coreActivity);
        Assert.NotNull(ia);
        Assert.Equal(TeamsActivityType.Invoke, ia.Type);
        Assert.Equal("testName", ia.Name);
        Assert.NotNull(ia.Value);
        Assert.Equal("convId", ia.Conversation?.Id);
        Assert.Equal("value", ia.Value?["key"]?.ToString());
    }
}
