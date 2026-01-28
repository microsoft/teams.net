// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Apps.Schema.MessageActivities;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.UnitTests;

public class InvokeActivityTest
{
    [Fact]
    public void DefaultCtor()
    {
        var ia = new InvokeActivity();
        Assert.NotNull(ia);
        Assert.Equal(TeamsActivityType.Invoke, ia.Type);
        Assert.Null(ia.Name);
        Assert.Null(ia.Value);
        // Assert.Null(ia.Conversation);
    }

    [Fact]
    public void FromCoreActivityWithValue()
    {
        var coreActivity = new CoreActivity
        {
            Type = TeamsActivityType.Invoke,
            Value = JsonNode.Parse("{ \"key\": \"value\" }"),
            Conversation = new Conversation { Id = "convId" },
            Properties = new ExtendedPropertiesDictionary
            {
                { "name", "testName" }
            }
        };
        var ia = InvokeActivity.FromActivity(coreActivity);
        Assert.NotNull(ia);
        Assert.Equal(TeamsActivityType.Invoke, ia.Type);
        Assert.Equal("testName", ia.Name);
        Assert.NotNull(ia.Value);
        Assert.Equal("convId", ia.Conversation?.Id);
        Assert.Equal("value", ia.Value?["key"]?.ToString());
    }
}
