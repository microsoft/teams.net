// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.State;

namespace Microsoft.Teams.Apps.UnitTests.State;

public class TurnStateTests
{
    private static TurnState Make() => new(
        new StateScope(persisted: false, loaded: null),
        new StateScope(persisted: false, loaded: null),
        new StateScope(persisted: false, loaded: null));

    [Fact]
    public void GetSetValue_RoutesByScopePrefix()
    {
        var ts = Make();

        ts.SetValue("conversation.a", 1);
        ts.SetValue("user.b", "x");
        ts.SetValue("temp.c", true);

        Assert.Equal(1, ts.Conversation.Get<int>("a"));
        Assert.Equal("x", ts.User.Get<string>("b"));
        Assert.True(ts.Temp.Get<bool>("c"));
    }

    [Fact]
    public void BarePath_DefaultsToTemp()
    {
        var ts = Make();

        ts.SetValue("loose", "v");

        Assert.Equal("v", ts.Temp.Get<string>("loose"));
        Assert.Equal("v", ts.GetValue<string>("temp.loose"));
    }

    [Fact]
    public void UnknownScopePrefix_Throws()
    {
        var ts = Make();

        Assert.Throws<ArgumentException>(() => ts.SetValue("bogus.k", 1));
        Assert.Throws<ArgumentException>(() => ts.GetValue<int>("bogus.k"));
    }
}
