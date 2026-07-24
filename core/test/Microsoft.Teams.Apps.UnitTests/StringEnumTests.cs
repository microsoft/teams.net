// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Teams.Apps;

namespace Microsoft.Teams.Apps.UnitTests;

public class StringEnumTests
{
    [Fact]
    public void StringEnum_ToString_ReturnsUnderlyingValue()
    {
        Assert.Equal("application/search", InvokeNames.Search.ToString());
    }

    [Fact]
    public void StringEnum_Equality_UsesValueSemantics()
    {
        InvokeName deserialized = JsonSerializer.Deserialize<InvokeName>("\"application/search\"")!;

        Assert.Equal(InvokeNames.Search, deserialized);
        Assert.True(deserialized == InvokeNames.Search);
        Assert.False(deserialized != InvokeNames.Search);
    }
}
