// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.UnitTests;

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
        CoreActivity coreActivity = new()
        {
            Type = TeamsActivityType.Invoke,
            Value = JsonNode.Parse("{ \"key\": \"value\" }"),
            Conversation = new Conversation { Id = "convId" },
            Properties = new ExtendedPropertiesDictionary
            {
                { "name", "testName" }
            }
        };
        InvokeActivity ia = InvokeActivity.FromActivity(coreActivity);
        Assert.NotNull(ia);
        Assert.Equal(TeamsActivityType.Invoke, ia.Type);
        Assert.Equal("testName", ia.Name);
        Assert.NotNull(ia.Value);
        Assert.Equal("convId", ia.Conversation?.Id);
        Assert.Equal("value", ia.Value?["key"]?.ToString());
    }
}
