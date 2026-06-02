// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Teams.Apps.State;

namespace Microsoft.Teams.Apps.UnitTests.State;

public class MemoryStorageTests
{
    private static StoreItem Item(params (string Key, object? Value)[] values)
    {
        var item = new StoreItem();
        foreach ((string key, object? value) in values)
        {
            item.Values[key] = value;
        }
        return item;
    }

    [Fact]
    public async Task WriteThenRead_RoundTrips()
    {
        var storage = new MemoryStorage();
        await storage.WriteAsync(new Dictionary<string, StoreItem> { ["k"] = Item(("n", 1)) });

        IReadOnlyDictionary<string, StoreItem> read = await storage.ReadAsync(["k"]);

        Assert.Equal(1, ((JsonElement)read["k"].Values["n"]!).GetInt32());
        Assert.NotNull(read["k"].ETag);
    }

    [Fact]
    public async Task ReadMissingKey_IsOmitted()
    {
        var storage = new MemoryStorage();
        Assert.Empty(await storage.ReadAsync(["nope"]));
    }

    [Fact]
    public async Task Delete_RemovesDocument()
    {
        var storage = new MemoryStorage();
        await storage.WriteAsync(new Dictionary<string, StoreItem> { ["k"] = Item(("n", 1)) });

        await storage.DeleteAsync(["k"]);

        Assert.Empty(await storage.ReadAsync(["k"]));
    }

    [Fact]
    public async Task Write_SnapshotsValues_IsolatingLaterMutation()
    {
        // MemoryStorage serializes on write, so mutating the caller's objects afterward must not leak in.
        var storage = new MemoryStorage();
        var list = new List<string> { "a" };
        var item = Item(("list", list));

        await storage.WriteAsync(new Dictionary<string, StoreItem> { ["k"] = item });
        list.Add("b");                 // mutate the list after write
        item.Values["list"] = null;    // and the item itself

        IReadOnlyDictionary<string, StoreItem> read = await storage.ReadAsync(["k"]);
        JsonElement stored = (JsonElement)read["k"].Values["list"]!;
        Assert.Equal(1, stored.GetArrayLength()); // still just ["a"]
    }

    [Fact]
    public async Task Rewrite_ChangesETag()
    {
        var storage = new MemoryStorage();
        await storage.WriteAsync(new Dictionary<string, StoreItem> { ["k"] = Item(("n", 1)) });
        string etag1 = (await storage.ReadAsync(["k"]))["k"].ETag!;

        await storage.WriteAsync(new Dictionary<string, StoreItem> { ["k"] = Item(("n", 2)) });
        string etag2 = (await storage.ReadAsync(["k"]))["k"].ETag!;

        Assert.NotEqual(etag1, etag2);
    }
}
