// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Teams.Apps.State;

namespace Microsoft.Teams.Apps.UnitTests.State;

public class StateScopeTests
{
    private static StateScope Persisted(IReadOnlyDictionary<string, object?>? loaded = null) => new(persisted: true, loaded);

    [Fact]
    public void SetThenGet_ReturnsValue()
    {
        var scope = Persisted();
        scope.Set("k", 42);
        Assert.Equal(42, scope.Get<int>("k"));
    }

    [Fact]
    public void Get_MissingKey_ReturnsDefault()
    {
        var scope = Persisted();
        Assert.Equal(0, scope.Get<int>("nope"));
        Assert.Null(scope.Get<string>("nope"));
    }

    [Fact]
    public void Remove_DeletesValue_AndReportsWhetherPresent()
    {
        var scope = Persisted();
        scope.Set("k", "v");
        Assert.True(scope.Remove("k"));
        Assert.False(scope.Remove("k"));
        Assert.False(scope.ContainsKey("k"));
    }

    [Fact]
    public void Clear_EmptiesScope()
    {
        var scope = Persisted();
        scope.Set("a", 1);
        scope.Set("b", 2);

        scope.Clear();

        Assert.True(scope.IsEmpty);
        Assert.False(scope.ContainsKey("a"));
    }

    [Fact]
    public void Get_FromJsonElement_Deserializes()
    {
        var loaded = StateSerializer.Deserialize("{\"n\":5}"u8);
        Assert.IsType<JsonElement>(loaded["n"]); // sanity: loaded as JsonElement

        var scope = Persisted(loaded);

        Assert.Equal(5, scope.Get<int>("n"));
    }

    [Fact]
    public void PureRead_DoesNotMarkChanged()
    {
        // Get caches the typed value back into the bag; re-serializing it must still match the baseline.
        var scope = Persisted(StateSerializer.Deserialize("{\"n\":5}"u8));

        scope.Get<int>("n");

        Assert.False(scope.IsChanged());
    }

    [Fact]
    public void Set_MarksPersistedScopeChanged()
    {
        var scope = Persisted();
        Assert.False(scope.IsChanged());

        scope.Set("k", 1);

        Assert.True(scope.IsChanged());
    }

    [Fact]
    public void NonPersistedScope_IsNeverChanged()
    {
        var scope = new StateScope(persisted: false, loaded: null);
        scope.Set("k", 1);
        Assert.False(scope.IsChanged());
    }

    [Fact]
    public void Snapshot_CopiesValues()
    {
        var scope = Persisted();
        scope.Set("k", "v");

        Dictionary<string, object?> values = scope.Snapshot();

        Assert.Equal("v", values["k"]);
    }

    [Fact]
    public void AfterComplete_EveryAccessThrows()
    {
        var scope = Persisted();
        scope.Set("k", 1);
        scope.Complete();

        Assert.Throws<InvalidOperationException>(() => scope.Get<int>("k"));
        Assert.Throws<InvalidOperationException>(() => scope.Set("k", 2));
        Assert.Throws<InvalidOperationException>(() => scope.Remove("k"));
        Assert.Throws<InvalidOperationException>(() => scope.ContainsKey("k"));
        Assert.Throws<InvalidOperationException>(() => scope.Clear());
    }
}
