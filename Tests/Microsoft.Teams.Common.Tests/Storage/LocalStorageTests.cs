
using Microsoft.Teams.Common.Storage;

namespace Microsoft.Teams.Common.Tests.Storage;
public class LocalStorageTests
{
    [Fact]
    public void LocalStorage_SizeAndKeys_WorkAsExpected()
    {
        var storage = new LocalStorage<int>();
        storage.Set("a", 1);
        storage.Set("b", 2);

        Assert.Equal(2, storage.Size);
        Assert.Contains("a", storage.Keys);
        Assert.Contains("b", storage.Keys);
    }

    [Fact]
    public void LocalStorage_ExistsAndGet_WorkAsExpected()
    {
        var storage = new LocalStorage<string>();
        storage.Set("foo", "bar");

        Assert.True(storage.Exists("foo"));
        Assert.Equal("bar", storage.Get("foo"));
        Assert.Null(storage.Get("baz"));
    }

    [Fact]
    public async Task LocalStorage_AsyncMethods_WorkAsExpected()
    {
        var storage = new LocalStorage<string>();
        await storage.SetAsync("foo", "bar");

        Assert.True(await storage.ExistsAsync("foo"));
        Assert.Equal("bar", await storage.GetAsync("foo"));
    }

    [Fact]
    public void LocalStorage_GetGeneric_ReturnsCorrectType()
    {
        var storage = new LocalStorage<object>();
        storage.Set("num", 42);

        int? value = storage.Get<int>("num");
        Assert.Equal(42, value);
    }

    [Fact]
    public async Task LocalStorage_GetAsyncGeneric_ReturnsCorrectType()
    {
        var storage = new LocalStorage<object>();
        await storage.SetAsync("num", 42);

        int? value = await storage.GetAsync<int>("num");
        Assert.Equal(42, value);
    }

    [Fact]
    public void LocalStorage_Delete_RemovesKey()
    {
        var storage = new LocalStorage<string>();
        storage.Set("foo", "bar");
        storage.Delete("foo");

        Assert.False(storage.Exists("foo"));
        Assert.Null(storage.Get("foo"));
    }

    [Fact]
    public async Task LocalStorage_DeleteAsync_RemovesKey()
    {
        var storage = new LocalStorage<string>();
        await storage.SetAsync("foo", "bar");
        await storage.DeleteAsync("foo");

        Assert.False(await storage.ExistsAsync("foo"));
        Assert.Null(await storage.GetAsync("foo"));
    }

    [Fact]
    public void LocalStorage_Eviction_WorksWhenMaxIsSet()
    {
        var storage = new LocalStorage<int>(max: 2);
        storage.Set("a", 1);
        storage.Set("b", 2);
        storage.Set("c", 3); // Should evict "a"

        Assert.False(storage.Exists("a"));
        Assert.True(storage.Exists("b"));
        Assert.True(storage.Exists("c"));
        Assert.Equal(2, storage.Size);
    }

    [Fact]
    public void LocalStorage_Hit_MovesKeyToEnd()
    {
        var storage = new LocalStorage<int>();
        storage.Set("a", 1);
        storage.Set("b", 2);
        storage.Set("c", 3);

        // Access "b" to move it to the end
        var result = storage.Get("b");
        Assert.Equal(2, result);
    }

    [Fact]
    public void LocalStorage_ConstructorWithData_InitializesCorrectly()
    {
        var data = new Dictionary<string, int> { { "x", 10 }, { "y", 20 } };
        var storage = new LocalStorage<int>(data, max: 5);

        Assert.True(storage.Exists("x"));
        Assert.True(storage.Exists("y"));
        Assert.Equal(2, storage.Size);
    }
}