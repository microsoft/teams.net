// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Teams.Apps.State;

namespace Microsoft.Teams.Apps.UnitTests.State;

public sealed class FileStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "teams-state-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

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
    public async Task WriteThenRead_RoundTripsValues()
    {
        var storage = new FileStorage(_root);
        const string key = "msteams/conversations/19:abc";
        await storage.WriteAsync(new Dictionary<string, StoreItem> { [key] = Item(("count", 1)) });

        IReadOnlyDictionary<string, StoreItem> read = await storage.ReadAsync([key]);

        Assert.True(read.TryGetValue(key, out StoreItem? item));
        JsonElement count = Assert.IsType<JsonElement>(item!.Values["count"]); // values come back as JsonElement
        Assert.Equal(1, count.GetInt32());
    }

    [Fact]
    public async Task ReadMissingKey_IsOmitted()
    {
        var storage = new FileStorage(_root);
        Assert.Empty(await storage.ReadAsync(["nope"]));
    }

    [Fact]
    public async Task Delete_RemovesDocument()
    {
        var storage = new FileStorage(_root);
        const string key = "msteams/users/user1";
        await storage.WriteAsync(new Dictionary<string, StoreItem> { [key] = Item(("name", "Bob")) });

        await storage.DeleteAsync([key]);

        Assert.Empty(await storage.ReadAsync([key]));
    }

    [Fact]
    public async Task DeleteMissingKey_DoesNotThrow()
    {
        var storage = new FileStorage(_root);
        await storage.DeleteAsync(["missing"]); // no exception
    }

    [Fact]
    public async Task Key_IsEncodedToSingleFlatFile()
    {
        var storage = new FileStorage(_root);
        const string key = "msteams/conversations/19:abc";
        await storage.WriteAsync(new Dictionary<string, StoreItem> { [key] = Item(("k", "v")) });

        // The key is percent-encoded into one flat file — no subdirectories from the '/' in the key.
        Assert.Empty(Directory.GetDirectories(_root));
        string[] files = Directory.GetFiles(_root, "*.json");
        Assert.Single(files);
        Assert.Equal(Uri.EscapeDataString(key) + ".json", Path.GetFileName(files[0]));

        // File holds the bare, compact, cross-runtime values document.
        Assert.Contains("\"k\":\"v\"", await File.ReadAllTextAsync(files[0]));
    }

    [Fact]
    public async Task PersistsAcrossInstances()
    {
        const string key = "msteams/users/user1";
        await new FileStorage(_root).WriteAsync(new Dictionary<string, StoreItem> { [key] = Item(("name", "Bob")) });

        // A fresh instance over the same directory sees the persisted document.
        IReadOnlyDictionary<string, StoreItem> read = await new FileStorage(_root).ReadAsync([key]);
        Assert.True(read.ContainsKey(key));
    }
}
