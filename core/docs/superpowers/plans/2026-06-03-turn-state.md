# Turn State Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add per-turn state management to BotApplication, backed by `IDistributedCache`, with key-value and typed object access.

**Architecture:** Core layer provides `ITurnState`, `TurnState`, `TurnStateMiddleware`, and DI registration. Apps layer adds a `State` accessor on `Context<TActivity>`. State is scoped per conversation+user, loaded/saved automatically by middleware, serialized as JSON to `IDistributedCache`.

**Tech Stack:** .NET 8/10, `Microsoft.Extensions.Caching.Abstractions`, `System.Text.Json`, xUnit, Moq

---

### Task 1: `ITurnState` Interface

**Files:**
- Create: `src/Microsoft.Teams.Core/State/ITurnState.cs`
- Test: `test/Microsoft.Teams.Core.UnitTests/State/TurnStateTests.cs`

- [ ] **Step 1: Create the interface**

Create `src/Microsoft.Teams.Core/State/ITurnState.cs`:

```csharp
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Core.State;

/// <summary>
/// Provides per-turn state storage with key-value and typed object access.
/// </summary>
public interface ITurnState
{
    /// <summary>
    /// Gets a value by key. Returns default if the key is not present or the value cannot be converted.
    /// </summary>
    T? Get<T>(string key);

    /// <summary>
    /// Sets a value by key.
    /// </summary>
    void Set<T>(string key, T value);

    /// <summary>
    /// Removes a key from state.
    /// </summary>
    void Remove(string key);

    /// <summary>
    /// Returns true if the key exists in state.
    /// </summary>
    bool ContainsKey(string key);

    /// <summary>
    /// Gets a typed state object. Creates a new instance via parameterless constructor if not present.
    /// </summary>
    T Get<T>() where T : class, new();

    /// <summary>
    /// Sets a typed state object, replacing any existing instance of the same type.
    /// </summary>
    void Set<T>(T value) where T : class;

    /// <summary>
    /// Returns true if a typed state object of this type exists.
    /// </summary>
    bool Has<T>() where T : class;

    /// <summary>
    /// Removes the typed state object of this type.
    /// </summary>
    void Remove<T>() where T : class;

    /// <summary>
    /// Returns true if any value has been added, modified, or removed since the state was loaded.
    /// </summary>
    bool IsDirty { get; }
}
```

- [ ] **Step 2: Commit**

```bash
git add src/Microsoft.Teams.Core/State/ITurnState.cs
git commit -m "feat(state): add ITurnState interface"
```

---

### Task 2: `TurnState` Implementation

**Files:**
- Create: `src/Microsoft.Teams.Core/State/TurnState.cs`
- Create: `test/Microsoft.Teams.Core.UnitTests/State/TurnStateTests.cs`

- [ ] **Step 1: Write failing tests**

Create `test/Microsoft.Teams.Core.UnitTests/State/TurnStateTests.cs`:

```csharp
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Core.State;

namespace Microsoft.Teams.Core.UnitTests.State;

public class TurnStateTests
{
    // ==================== Key-Value Tests ====================

    [Fact]
    public void Get_MissingKey_ReturnsDefault()
    {
        TurnState state = new();
        Assert.Equal(0, state.Get<int>("missing"));
        Assert.Null(state.Get<string>("missing"));
    }

    [Fact]
    public void Set_ThenGet_ReturnsValue()
    {
        TurnState state = new();
        state.Set("count", 42);
        Assert.Equal(42, state.Get<int>("count"));
    }

    [Fact]
    public void Set_OverwritesExistingKey()
    {
        TurnState state = new();
        state.Set("key", "first");
        state.Set("key", "second");
        Assert.Equal("second", state.Get<string>("key"));
    }

    [Fact]
    public void Remove_ExistingKey_RemovesIt()
    {
        TurnState state = new();
        state.Set("key", "value");
        state.Remove("key");
        Assert.False(state.ContainsKey("key"));
    }

    [Fact]
    public void Remove_MissingKey_DoesNotThrow()
    {
        TurnState state = new();
        state.Remove("missing");
    }

    [Fact]
    public void ContainsKey_ReturnsCorrectly()
    {
        TurnState state = new();
        Assert.False(state.ContainsKey("key"));
        state.Set("key", "value");
        Assert.True(state.ContainsKey("key"));
    }

    // ==================== Typed Object Tests ====================

    [Fact]
    public void GetTyped_NoExisting_CreatesNewInstance()
    {
        TurnState state = new();
        TestUserPrefs prefs = state.Get<TestUserPrefs>();
        Assert.NotNull(prefs);
        Assert.Equal("light", prefs.Theme);
    }

    [Fact]
    public void SetTyped_ThenGetTyped_ReturnsSameInstance()
    {
        TurnState state = new();
        TestUserPrefs prefs = new() { Theme = "dark" };
        state.Set(prefs);
        TestUserPrefs retrieved = state.Get<TestUserPrefs>();
        Assert.Equal("dark", retrieved.Theme);
    }

    [Fact]
    public void HasTyped_ReturnsFalseWhenMissing()
    {
        TurnState state = new();
        Assert.False(state.Has<TestUserPrefs>());
    }

    [Fact]
    public void HasTyped_ReturnsTrueAfterSet()
    {
        TurnState state = new();
        state.Set(new TestUserPrefs());
        Assert.True(state.Has<TestUserPrefs>());
    }

    [Fact]
    public void RemoveTyped_RemovesInstance()
    {
        TurnState state = new();
        state.Set(new TestUserPrefs());
        state.Remove<TestUserPrefs>();
        Assert.False(state.Has<TestUserPrefs>());
    }

    [Fact]
    public void MultipleTypedObjects_AreIndependent()
    {
        TurnState state = new();
        state.Set(new TestUserPrefs { Theme = "dark" });
        state.Set(new TestDialogState { CurrentStep = 3 });

        Assert.Equal("dark", state.Get<TestUserPrefs>().Theme);
        Assert.Equal(3, state.Get<TestDialogState>().CurrentStep);
    }

    // ==================== Dirty Tracking Tests ====================

    [Fact]
    public void IsDirty_FalseInitially()
    {
        TurnState state = new();
        Assert.False(state.IsDirty);
    }

    [Fact]
    public void IsDirty_TrueAfterSet()
    {
        TurnState state = new();
        state.Set("key", "value");
        Assert.True(state.IsDirty);
    }

    [Fact]
    public void IsDirty_TrueAfterRemove_WhenKeyExists()
    {
        TurnState state = new();
        state.Set("key", "value");
        state.IsDirty.ToString(); // read to confirm true
        // Reset dirty by creating fresh and loading
        TurnState state2 = TurnState.FromDictionary(
            new Dictionary<string, object?> { ["key"] = "value" });
        Assert.False(state2.IsDirty);
        state2.Remove("key");
        Assert.True(state2.IsDirty);
    }

    [Fact]
    public void IsDirty_TrueAfterTypedSet()
    {
        TurnState state = new();
        state.Set(new TestUserPrefs());
        Assert.True(state.IsDirty);
    }

    // ==================== Serialization Tests ====================

    [Fact]
    public void ToJsonBytes_ThenFromJsonBytes_RoundTrips()
    {
        TurnState state = new();
        state.Set("count", 42);
        state.Set("name", "bot");

        byte[] bytes = state.ToJsonBytes();
        TurnState restored = TurnState.FromJsonBytes(bytes);

        Assert.Equal(42, restored.Get<int>("count"));
        Assert.Equal("bot", restored.Get<string>("name"));
        Assert.False(restored.IsDirty);
    }

    [Fact]
    public void FromJsonBytes_EmptyArray_ReturnsEmptyState()
    {
        TurnState state = TurnState.FromJsonBytes([]);
        Assert.False(state.IsDirty);
        Assert.Null(state.Get<string>("any"));
    }

    [Fact]
    public void FromJsonBytes_Null_ReturnsEmptyState()
    {
        TurnState state = TurnState.FromJsonBytes(null);
        Assert.False(state.IsDirty);
    }

    // ==================== Test Types ====================

    public class TestUserPrefs
    {
        public string Theme { get; set; } = "light";
    }

    public class TestDialogState
    {
        public int CurrentStep { get; set; }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
cd C:/_code/core-teams.net && dotnet test core/test/Microsoft.Teams.Core.UnitTests --filter "FullyQualifiedName~TurnStateTests" --no-restore -v q
```

Expected: Build failure — `TurnState` class does not exist yet.

- [ ] **Step 3: Implement TurnState**

Create `src/Microsoft.Teams.Core/State/TurnState.cs`:

```csharp
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Core.State;

/// <summary>
/// Default implementation of <see cref="ITurnState"/> backed by a dictionary.
/// Supports key-value and typed object access with dirty tracking.
/// </summary>
public sealed class TurnState : ITurnState
{
    private static readonly string TypedKeyPrefix = "$";

    private readonly Dictionary<string, object?> _data;
    private bool _isDirty;

    /// <summary>
    /// Creates a new empty <see cref="TurnState"/>.
    /// </summary>
    public TurnState()
    {
        _data = [];
    }

    private TurnState(Dictionary<string, object?> data)
    {
        _data = data;
    }

    /// <inheritdoc />
    public bool IsDirty => _isDirty;

    // ==================== Key-Value Access ====================

    /// <inheritdoc />
    public T? Get<T>(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (!_data.TryGetValue(key, out object? value) || value is null)
        {
            return default;
        }

        if (value is T typed)
        {
            return typed;
        }

        // Handle JsonElement from deserialization
        if (value is JsonElement element)
        {
            return element.Deserialize<T>();
        }

        return (T)Convert.ChangeType(value, typeof(T));
    }

    /// <inheritdoc />
    public void Set<T>(string key, T value)
    {
        ArgumentNullException.ThrowIfNull(key);
        _data[key] = value;
        _isDirty = true;
    }

    /// <inheritdoc />
    public void Remove(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (_data.Remove(key))
        {
            _isDirty = true;
        }
    }

    /// <inheritdoc />
    public bool ContainsKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _data.ContainsKey(key);
    }

    // ==================== Typed Object Access ====================

    private static string TypeKey<T>() => $"{TypedKeyPrefix}{typeof(T).Name}";

    /// <inheritdoc />
    public T Get<T>() where T : class, new()
    {
        string key = TypeKey<T>();
        if (_data.TryGetValue(key, out object? value) && value is not null)
        {
            if (value is T typed)
            {
                return typed;
            }

            if (value is JsonElement element)
            {
                T deserialized = element.Deserialize<T>() ?? new T();
                _data[key] = deserialized;
                return deserialized;
            }
        }

        T instance = new();
        _data[key] = instance;
        _isDirty = true;
        return instance;
    }

    /// <inheritdoc />
    public void Set<T>(T value) where T : class
    {
        ArgumentNullException.ThrowIfNull(value);
        _data[TypeKey<T>()] = value;
        _isDirty = true;
    }

    /// <inheritdoc />
    public bool Has<T>() where T : class => _data.ContainsKey(TypeKey<T>());

    /// <inheritdoc />
    public void Remove<T>() where T : class
    {
        if (_data.Remove(TypeKey<T>()))
        {
            _isDirty = true;
        }
    }

    // ==================== Serialization ====================

    /// <summary>
    /// Serializes the state to a JSON byte array for cache storage.
    /// </summary>
    public byte[] ToJsonBytes()
    {
        return JsonSerializer.SerializeToUtf8Bytes(_data);
    }

    /// <summary>
    /// Deserializes state from a JSON byte array. Returns a clean (non-dirty) state.
    /// </summary>
    public static TurnState FromJsonBytes(byte[]? bytes)
    {
        if (bytes is null || bytes.Length == 0)
        {
            return new TurnState();
        }

        Dictionary<string, object?>? data = JsonSerializer.Deserialize<Dictionary<string, object?>>(bytes);
        return new TurnState(data ?? []);
    }

    /// <summary>
    /// Creates a <see cref="TurnState"/> from an existing dictionary. Returns a clean (non-dirty) state.
    /// Used for testing.
    /// </summary>
    public static TurnState FromDictionary(Dictionary<string, object?> data)
    {
        return new TurnState(new Dictionary<string, object?>(data));
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
cd C:/_code/core-teams.net && dotnet test core/test/Microsoft.Teams.Core.UnitTests --filter "FullyQualifiedName~TurnStateTests" --no-restore -v q
```

Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Teams.Core/State/TurnState.cs test/Microsoft.Teams.Core.UnitTests/State/TurnStateTests.cs
git commit -m "feat(state): add TurnState implementation with tests"
```

---

### Task 3: `TurnStateOptions`

**Files:**
- Create: `src/Microsoft.Teams.Core/State/TurnStateOptions.cs`

- [ ] **Step 1: Create the options class**

Create `src/Microsoft.Teams.Core/State/TurnStateOptions.cs`:

```csharp
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Caching.Distributed;

namespace Microsoft.Teams.Core.State;

/// <summary>
/// Configuration options for turn state management.
/// </summary>
public class TurnStateOptions
{
    /// <summary>
    /// Gets or sets the cache entry options applied when saving state.
    /// Defaults to a 1-hour sliding expiration.
    /// </summary>
    public DistributedCacheEntryOptions CacheEntryOptions { get; set; } = new()
    {
        SlidingExpiration = TimeSpan.FromHours(1)
    };
}
```

- [ ] **Step 2: Add `Microsoft.Extensions.Caching.Abstractions` package reference**

Add to `src/Microsoft.Teams.Core/Microsoft.Teams.Core.csproj` inside the unconditional `<ItemGroup>` that has the other package references:

```xml
<PackageReference Include="Microsoft.Extensions.Caching.Abstractions" Version="9.0.6" />
```

- [ ] **Step 3: Verify it builds**

```bash
cd C:/_code/core-teams.net && dotnet build core/src/Microsoft.Teams.Core --no-restore -v q
```

Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
git add src/Microsoft.Teams.Core/State/TurnStateOptions.cs src/Microsoft.Teams.Core/Microsoft.Teams.Core.csproj
git commit -m "feat(state): add TurnStateOptions and caching dependency"
```

---

### Task 4: `TurnStateMiddleware`

**Files:**
- Create: `src/Microsoft.Teams.Core/State/TurnStateMiddleware.cs`
- Create: `test/Microsoft.Teams.Core.UnitTests/State/TurnStateMiddlewareTests.cs`

- [ ] **Step 1: Write failing tests**

Create `test/Microsoft.Teams.Core.UnitTests/State/TurnStateMiddlewareTests.cs`:

```csharp
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.Teams.Core.Schema;
using Microsoft.Teams.Core.State;
using Moq;

namespace Microsoft.Teams.Core.UnitTests.State;

public class TurnStateMiddlewareTests
{
    private readonly Mock<IDistributedCache> _mockCache = new();
    private readonly TurnStateOptions _options = new();

    private TurnStateMiddleware CreateMiddleware()
    {
        return new TurnStateMiddleware(_mockCache.Object, Options.Create(_options));
    }

    private static BotApplication CreateBotApp() => new TestBotApplication();

    private static CoreActivity CreateActivity(string conversationId = "conv1", string userId = "user1")
    {
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Conversation = new(conversationId),
            From = new() { Id = userId }
        };
        return activity;
    }

    [Fact]
    public async Task OnTurnAsync_LoadsState_BeforeNextTurn()
    {
        TurnStateMiddleware middleware = CreateMiddleware();
        BotApplication botApp = CreateBotApp();
        CoreActivity activity = CreateActivity();

        bool stateWasAvailable = false;
        await middleware.OnTurnAsync(botApp, activity, async (ct) =>
        {
            stateWasAvailable = botApp.TurnState is not null;
        });

        Assert.True(stateWasAvailable);
    }

    [Fact]
    public async Task OnTurnAsync_ClearsState_AfterNextTurn()
    {
        TurnStateMiddleware middleware = CreateMiddleware();
        BotApplication botApp = CreateBotApp();
        CoreActivity activity = CreateActivity();

        await middleware.OnTurnAsync(botApp, activity, (ct) => Task.CompletedTask);

        Assert.Null(botApp.TurnState);
    }

    [Fact]
    public async Task OnTurnAsync_ClearsState_WhenNextTurnThrows()
    {
        TurnStateMiddleware middleware = CreateMiddleware();
        BotApplication botApp = CreateBotApp();
        CoreActivity activity = CreateActivity();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            middleware.OnTurnAsync(botApp, activity, (ct) =>
                throw new InvalidOperationException("handler error")));

        Assert.Null(botApp.TurnState);
    }

    [Fact]
    public async Task OnTurnAsync_SavesDirtyState()
    {
        TurnStateMiddleware middleware = CreateMiddleware();
        BotApplication botApp = CreateBotApp();
        CoreActivity activity = CreateActivity();

        await middleware.OnTurnAsync(botApp, activity, (ct) =>
        {
            botApp.TurnState!.Set("key", "value");
            return Task.CompletedTask;
        });

        _mockCache.Verify(c => c.SetAsync(
            "ts:conv1:user1",
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnTurnAsync_SkipsSave_WhenNotDirty()
    {
        TurnStateMiddleware middleware = CreateMiddleware();
        BotApplication botApp = CreateBotApp();
        CoreActivity activity = CreateActivity();

        await middleware.OnTurnAsync(botApp, activity, (ct) => Task.CompletedTask);

        _mockCache.Verify(c => c.SetAsync(
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnTurnAsync_LoadsExistingState_FromCache()
    {
        TurnState existing = new();
        existing.Set("greeting", "hello");
        byte[] cached = existing.ToJsonBytes();

        _mockCache.Setup(c => c.GetAsync("ts:conv1:user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        TurnStateMiddleware middleware = CreateMiddleware();
        BotApplication botApp = CreateBotApp();
        CoreActivity activity = CreateActivity();

        string? loadedValue = null;
        await middleware.OnTurnAsync(botApp, activity, (ct) =>
        {
            loadedValue = botApp.TurnState!.Get<string>("greeting");
            return Task.CompletedTask;
        });

        Assert.Equal("hello", loadedValue);
    }

    [Fact]
    public async Task OnTurnAsync_SkipsState_WhenConversationIsNull()
    {
        TurnStateMiddleware middleware = CreateMiddleware();
        BotApplication botApp = CreateBotApp();
        CoreActivity activity = new() { Type = ActivityType.Message };

        bool nextCalled = false;
        await middleware.OnTurnAsync(botApp, activity, (ct) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
        Assert.Null(botApp.TurnState);
    }

    [Fact]
    public async Task OnTurnAsync_SkipsState_WhenFromIsNull()
    {
        TurnStateMiddleware middleware = CreateMiddleware();
        BotApplication botApp = CreateBotApp();
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Conversation = new("conv1")
        };

        bool nextCalled = false;
        await middleware.OnTurnAsync(botApp, activity, (ct) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
        Assert.Null(botApp.TurnState);
    }

    [Fact]
    public async Task SessionKey_UsesConversationAndFromIds()
    {
        _mockCache.Setup(c => c.GetAsync("ts:my-conv:my-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        TurnStateMiddleware middleware = CreateMiddleware();
        BotApplication botApp = CreateBotApp();
        CoreActivity activity = CreateActivity("my-conv", "my-user");

        await middleware.OnTurnAsync(botApp, activity, (ct) =>
        {
            botApp.TurnState!.Set("x", 1);
            return Task.CompletedTask;
        });

        _mockCache.Verify(c => c.SetAsync(
            "ts:my-conv:my-user",
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// A minimal BotApplication subclass for testing that does not require DI.
    /// </summary>
    private sealed class TestBotApplication : BotApplication
    {
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
cd C:/_code/core-teams.net && dotnet test core/test/Microsoft.Teams.Core.UnitTests --filter "FullyQualifiedName~TurnStateMiddlewareTests" --no-restore -v q
```

Expected: Build failure — `TurnStateMiddleware` does not exist, and `BotApplication.TurnState` property does not exist.

- [ ] **Step 3: Add `TurnState` property to `BotApplication`**

In `src/Microsoft.Teams.Core/BotApplication.cs`, add after line 165 (the `OnActivity` property):

```csharp
    /// <summary>
    /// Gets the per-turn state for the current activity, if state management is enabled.
    /// This property is set by <see cref="State.TurnStateMiddleware"/> at the start of each turn
    /// and cleared after the turn completes.
    /// </summary>
    /// <remarks>
    /// Returns <see langword="null"/> when state management is not configured.
    /// Call <c>AddBotApplicationState()</c> during service registration to enable state management.
    /// </remarks>
    public ITurnState? TurnState { get; internal set; }
```

Add `using Microsoft.Teams.Core.State;` to the top of the file.

- [ ] **Step 4: Implement `TurnStateMiddleware`**

Create `src/Microsoft.Teams.Core/State/TurnStateMiddleware.cs`:

```csharp
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Core.State;

/// <summary>
/// Middleware that loads per-turn state from <see cref="IDistributedCache"/> before the turn
/// and saves it back after the turn completes (if dirty).
/// </summary>
public sealed class TurnStateMiddleware : ITurnMiddleware
{
    private readonly IDistributedCache _cache;
    private readonly TurnStateOptions _options;

    /// <summary>
    /// Initializes a new instance of <see cref="TurnStateMiddleware"/>.
    /// </summary>
    public TurnStateMiddleware(IDistributedCache cache, IOptions<TurnStateOptions> options)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(options);
        _cache = cache;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task OnTurnAsync(BotApplication botApplication, CoreActivity activity, NextTurn nextTurn, CancellationToken cancellationToken = default)
    {
        string? sessionKey = GetSessionKey(activity);

        if (sessionKey is null)
        {
            await nextTurn(cancellationToken).ConfigureAwait(false);
            return;
        }

        byte[]? cached = await _cache.GetAsync(sessionKey, cancellationToken).ConfigureAwait(false);
        TurnState state = TurnState.FromJsonBytes(cached);
        botApplication.TurnState = state;

        try
        {
            await nextTurn(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (state.IsDirty)
            {
                byte[] bytes = state.ToJsonBytes();
                await _cache.SetAsync(sessionKey, bytes, _options.CacheEntryOptions, cancellationToken).ConfigureAwait(false);
            }

            botApplication.TurnState = null;
        }
    }

    internal static string? GetSessionKey(CoreActivity activity)
    {
        string? conversationId = activity.Conversation?.Id;
        string? userId = activity.From?.Id;

        if (string.IsNullOrEmpty(conversationId) || string.IsNullOrEmpty(userId))
        {
            return null;
        }

        return $"ts:{conversationId}:{userId}";
    }
}
```

- [ ] **Step 5: Add `Microsoft.Extensions.Caching.Abstractions` to test project**

Add to `test/Microsoft.Teams.Core.UnitTests/Microsoft.Teams.Core.UnitTests.csproj`:

```xml
<PackageReference Include="Microsoft.Extensions.Caching.Abstractions" Version="9.0.6" />
<PackageReference Include="Microsoft.Extensions.Options" Version="9.0.6" />
```

- [ ] **Step 6: Run tests to verify they pass**

```bash
cd C:/_code/core-teams.net && dotnet test core/test/Microsoft.Teams.Core.UnitTests --filter "FullyQualifiedName~TurnStateMiddlewareTests" --no-restore -v q
```

Expected: All tests pass.

- [ ] **Step 7: Commit**

```bash
git add src/Microsoft.Teams.Core/State/TurnStateMiddleware.cs src/Microsoft.Teams.Core/BotApplication.cs test/Microsoft.Teams.Core.UnitTests/State/TurnStateMiddlewareTests.cs test/Microsoft.Teams.Core.UnitTests/Microsoft.Teams.Core.UnitTests.csproj
git commit -m "feat(state): add TurnStateMiddleware and BotApplication.TurnState property"
```

---

### Task 5: DI Registration Extension

**Files:**
- Modify: `src/Microsoft.Teams.Core/Hosting/AddBotApplicationExtensions.cs`
- Create: `test/Microsoft.Teams.Core.UnitTests/State/AddBotApplicationStateTests.cs`

- [ ] **Step 1: Write failing tests**

Create `test/Microsoft.Teams.Core.UnitTests/State/AddBotApplicationStateTests.cs`:

```csharp
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Teams.Core.Hosting;
using Microsoft.Teams.Core.State;

namespace Microsoft.Teams.Core.UnitTests.State;

public class AddBotApplicationStateTests
{
    [Fact]
    public void AddBotApplicationState_RegistersMiddleware()
    {
        ServiceCollection services = new();
        services.AddSingleton<IDistributedCache>(new TestDistributedCache());
        services.AddBotApplicationState();

        ServiceProvider provider = services.BuildServiceProvider();
        TurnStateMiddleware? middleware = provider.GetService<TurnStateMiddleware>();

        Assert.NotNull(middleware);
    }

    [Fact]
    public void AddBotApplicationState_RegistersDefaultOptions()
    {
        ServiceCollection services = new();
        services.AddSingleton<IDistributedCache>(new TestDistributedCache());
        services.AddBotApplicationState();

        ServiceProvider provider = services.BuildServiceProvider();
        IOptions<TurnStateOptions> options = provider.GetRequiredService<IOptions<TurnStateOptions>>();

        Assert.NotNull(options.Value);
        Assert.Equal(TimeSpan.FromHours(1), options.Value.CacheEntryOptions.SlidingExpiration);
    }

    [Fact]
    public void AddBotApplicationState_AcceptsCustomOptions()
    {
        ServiceCollection services = new();
        services.AddSingleton<IDistributedCache>(new TestDistributedCache());
        services.AddBotApplicationState(options =>
        {
            options.CacheEntryOptions.SlidingExpiration = TimeSpan.FromMinutes(30);
        });

        ServiceProvider provider = services.BuildServiceProvider();
        IOptions<TurnStateOptions> options = provider.GetRequiredService<IOptions<TurnStateOptions>>();

        Assert.Equal(TimeSpan.FromMinutes(30), options.Value.CacheEntryOptions.SlidingExpiration);
    }

    private sealed class TestDistributedCache : IDistributedCache
    {
        public byte[]? Get(string key) => null;
        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Task.FromResult<byte[]?>(null);
        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) { }
        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) => Task.CompletedTask;
        public void Refresh(string key) { }
        public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;
        public void Remove(string key) { }
        public Task RemoveAsync(string key, CancellationToken token = default) => Task.CompletedTask;
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
cd C:/_code/core-teams.net && dotnet test core/test/Microsoft.Teams.Core.UnitTests --filter "FullyQualifiedName~AddBotApplicationStateTests" --no-restore -v q
```

Expected: Build failure — `AddBotApplicationState` method does not exist.

- [ ] **Step 3: Implement `AddBotApplicationState` extension method**

In `src/Microsoft.Teams.Core/Hosting/AddBotApplicationExtensions.cs`, add the following method to the `AddBotApplicationExtensions` class. Add `using Microsoft.Teams.Core.State;` to the top of the file.

```csharp
    /// <summary>
    /// Registers turn state management services.
    /// The caller must register an <see cref="IDistributedCache"/> implementation separately
    /// (e.g., <c>services.AddDistributedMemoryCache()</c> or a Redis/SQL provider).
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configure">Optional callback to configure <see cref="TurnStateOptions"/>.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddBotApplicationState(
        this IServiceCollection services,
        Action<TurnStateOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.AddOptions<TurnStateOptions>();
        }

        services.AddSingleton<TurnStateMiddleware>();
        return services;
    }
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
cd C:/_code/core-teams.net && dotnet test core/test/Microsoft.Teams.Core.UnitTests --filter "FullyQualifiedName~AddBotApplicationStateTests" --no-restore -v q
```

Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Teams.Core/Hosting/AddBotApplicationExtensions.cs test/Microsoft.Teams.Core.UnitTests/State/AddBotApplicationStateTests.cs
git commit -m "feat(state): add AddBotApplicationState DI extension"
```

---

### Task 6: `Context<TActivity>.State` Accessor

**Files:**
- Modify: `src/Microsoft.Teams.Apps/Context.cs`
- Create: `test/Microsoft.Teams.Apps.UnitTests/ContextStateTests.cs`

- [ ] **Step 1: Write failing tests**

Create `test/Microsoft.Teams.Apps.UnitTests/ContextStateTests.cs`. First check how `Context` is constructed in existing tests to follow the same pattern.

The `Context` constructor takes `(TeamsBotApplication, TActivity)`. The `TeamsBotApplication` extends `BotApplication`. We need a minimal test setup:

```csharp
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core.State;

namespace Microsoft.Teams.Apps.UnitTests;

public class ContextStateTests
{
    [Fact]
    public void State_WhenTurnStateIsSet_ReturnsIt()
    {
        TestTeamsBotApp botApp = new();
        TurnState state = new();
        botApp.TurnState = state;

        Context<TeamsActivity> context = new(botApp, new TeamsActivity());
        Assert.Same(state, context.State);
    }

    [Fact]
    public void State_WhenTurnStateIsNull_Throws()
    {
        TestTeamsBotApp botApp = new();

        Context<TeamsActivity> context = new(botApp, new TeamsActivity());
        Assert.Throws<InvalidOperationException>(() => context.State);
    }

    private sealed class TestTeamsBotApp : TeamsBotApplication
    {
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
cd C:/_code/core-teams.net && dotnet test core/test/Microsoft.Teams.Apps.UnitTests --filter "FullyQualifiedName~ContextStateTests" --no-restore -v q
```

Expected: Build failure — `Context<T>.State` property does not exist.

- [ ] **Step 3: Add `State` property to `Context<TActivity>`**

In `src/Microsoft.Teams.Apps/Context.cs`, add `using Microsoft.Teams.Core.State;` to the top of the file.

Add the following property after the `Api` property (after line 51):

```csharp
    // ==================== Turn State ====================

    /// <summary>
    /// Gets the per-turn state for the current activity.
    /// Requires <c>AddBotApplicationState()</c> to be called during service registration.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when state management is not configured.</exception>
    public ITurnState State => TeamsBotApplication.TurnState
        ?? throw new InvalidOperationException(
            "TurnState is not available. Call AddBotApplicationState() during service registration.");
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
cd C:/_code/core-teams.net && dotnet test core/test/Microsoft.Teams.Apps.UnitTests --filter "FullyQualifiedName~ContextStateTests" --no-restore -v q
```

Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Teams.Apps/Context.cs test/Microsoft.Teams.Apps.UnitTests/ContextStateTests.cs
git commit -m "feat(state): add State accessor to Context<TActivity>"
```

---

### Task 7: Full Build and Existing Test Regression

**Files:** None (validation only)

- [ ] **Step 1: Build the entire solution**

```bash
cd C:/_code/core-teams.net && dotnet build Microsoft.Teams.sln -v q
```

Expected: Build succeeds with no errors.

- [ ] **Step 2: Run all unit tests**

```bash
cd C:/_code/core-teams.net && dotnet test Microsoft.Teams.sln --no-build -v q
```

Expected: All tests pass, no regressions.

- [ ] **Step 3: Commit (if any fixups were needed)**

Only commit if fixes were required. Otherwise, skip.
