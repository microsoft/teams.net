// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Teams.Apps.State;

namespace Microsoft.Teams.Apps.UnitTests.State;

public class StateSerializerTests
{
    [Fact]
    public void RoundTrip_ValuesComeBackAsJsonElement()
    {
        string json = StateSerializer.Serialize(new Dictionary<string, object?> { ["n"] = 5, ["s"] = "x" });
        Dictionary<string, object?> back = StateSerializer.Deserialize(json);

        Assert.Equal(5, ((JsonElement)back["n"]!).GetInt32());
        Assert.Equal("x", ((JsonElement)back["s"]!).GetString());
    }

    [Fact]
    public void Serialize_UsesCamelCase_ForUserPoco()
    {
        string json = StateSerializer.Serialize(new Dictionary<string, object?> { ["p"] = new Preference("Bob", true) });

        Assert.Contains("\"displayName\":\"Bob\"", json);
        Assert.Contains("\"darkMode\":true", json);
    }

    [Fact]
    public void Convert_DeserializesJsonElementToType()
    {
        Dictionary<string, object?> bag = StateSerializer.Deserialize("{\"n\":7}");

        Assert.Equal(7, StateSerializer.Convert<int>((JsonElement)bag["n"]!));
    }

    [Fact]
    public void UserPoco_RoundTripsViaReflectionFallback()
    {
        // The user record is not in the source-gen context — this exercises the reflection resolver.
        string json = StateSerializer.Serialize(new Dictionary<string, object?> { ["p"] = new Preference("Bob", true) });
        Dictionary<string, object?> back = StateSerializer.Deserialize(json);

        Assert.Equal(new Preference("Bob", true), StateSerializer.Convert<Preference>((JsonElement)back["p"]!));
    }

    private sealed record Preference(string DisplayName, bool DarkMode);
}
