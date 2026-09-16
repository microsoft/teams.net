// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Teams.Apps.State;
using Moq;

namespace Microsoft.Teams.Apps.UnitTests.State;

public class TurnStateLoaderTests
{
    private readonly Mock<IDistributedCache> _cacheMock = new();
    private readonly TurnStateOptions _options = new();

    private TurnStateLoader CreateLoader() =>
        new(_cacheMock.Object, Options.Create(_options), NullLogger<TurnStateLoader>.Instance);

    // ── Load behavior ─────────────────────────────────────────────────

    [Fact]
    public async Task LoadAsync_ReturnsConversationAndUserState()
    {
        TurnStateLoader loader = CreateLoader();

        TurnStateContainer container = await loader.LoadAsync("conv1", "user1", CancellationToken.None);

        Assert.NotNull(container.ConversationState);
        Assert.NotNull(container.UserState);
    }

    [Fact]
    public async Task LoadAsync_NullUserId_ReturnsNullUserState()
    {
        TurnStateLoader loader = CreateLoader();

        TurnStateContainer container = await loader.LoadAsync("conv1", null, CancellationToken.None);

        Assert.NotNull(container.ConversationState);
        Assert.Null(container.UserState);
    }

    [Fact]
    public async Task LoadAsync_EmptyUserId_ReturnsNullUserState()
    {
        TurnStateLoader loader = CreateLoader();

        TurnStateContainer container = await loader.LoadAsync("conv1", "", CancellationToken.None);

        Assert.NotNull(container.ConversationState);
        Assert.Null(container.UserState);
    }

    [Fact]
    public async Task LoadAsync_ExistingConversationState_IsLoaded()
    {
        TurnState existing = new();
        existing.Set("shared", "hello");

        _cacheMock.Setup(c => c.GetAsync("ts:conv:conv1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing.ToJsonBytes());

        TurnStateLoader loader = CreateLoader();
        TurnStateContainer container = await loader.LoadAsync("conv1", "user1", CancellationToken.None);

        Assert.Equal("hello", container.ConversationState.Get<string>("shared"));
    }

    [Fact]
    public async Task LoadAsync_ExistingUserState_IsLoaded()
    {
        TurnState existing = new();
        existing.Set("private", "world");

        _cacheMock.Setup(c => c.GetAsync("ts:user:conv1:user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing.ToJsonBytes());

        TurnStateLoader loader = CreateLoader();
        TurnStateContainer container = await loader.LoadAsync("conv1", "user1", CancellationToken.None);

        Assert.Equal("world", container.UserState!.Get<string>("private"));
    }

    // ── Save behavior ─────────────────────────────────────────────────

    [Fact]
    public async Task SaveAsync_DirtyConversationState_IsSaved()
    {
        TurnStateLoader loader = CreateLoader();
        TurnStateContainer container = await loader.LoadAsync("conv1", "user1", CancellationToken.None);

        container.ConversationState.Set("key", "value");

        await loader.SaveAsync(container, "conv1", "user1", CancellationToken.None);

        _cacheMock.Verify(c => c.SetAsync(
            "ts:conv:conv1",
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_DirtyUserState_IsSaved()
    {
        TurnStateLoader loader = CreateLoader();
        TurnStateContainer container = await loader.LoadAsync("conv1", "user1", CancellationToken.None);

        container.UserState!.Set("key", "value");

        await loader.SaveAsync(container, "conv1", "user1", CancellationToken.None);

        _cacheMock.Verify(c => c.SetAsync(
            "ts:user:conv1:user1",
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_EmptyDirtyUserState_IsRemoved()
    {
        TurnStateLoader loader = CreateLoader();
        TurnStateContainer container = await loader.LoadAsync("conv1", "user1", CancellationToken.None);

        container.UserState!.Set("key", "value");
        container.UserState.Clear();

        await loader.SaveAsync(container, "conv1", "user1", CancellationToken.None);

        _cacheMock.Verify(c => c.RemoveAsync("ts:user:conv1:user1", It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.SetAsync(
            "ts:user:conv1:user1",
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_EmptyDirtyConversationState_IsRemoved()
    {
        TurnStateLoader loader = CreateLoader();
        TurnStateContainer container = await loader.LoadAsync("conv1", "user1", CancellationToken.None);

        container.ConversationState.Set("key", "value");
        container.ConversationState.Clear();

        await loader.SaveAsync(container, "conv1", "user1", CancellationToken.None);

        _cacheMock.Verify(c => c.RemoveAsync("ts:conv:conv1", It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.SetAsync(
            "ts:conv:conv1",
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_NonDirtyState_IsNotSaved()
    {
        TurnStateLoader loader = CreateLoader();
        TurnStateContainer container = await loader.LoadAsync("conv1", "user1", CancellationToken.None);

        await loader.SaveAsync(container, "conv1", "user1", CancellationToken.None);

        _cacheMock.Verify(c => c.SetAsync(
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Key format ────────────────────────────────────────────────────

    [Fact]
    public async Task KeysUseCorrectFormat()
    {
        TurnStateLoader loader = CreateLoader();
        TurnStateContainer container = await loader.LoadAsync("myConv", "myUser", CancellationToken.None);

        container.ConversationState.Set("x", 1);
        container.UserState!.Set("y", 2);

        await loader.SaveAsync(container, "myConv", "myUser", CancellationToken.None);

        _cacheMock.Verify(c => c.SetAsync(
            "ts:conv:myConv",
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _cacheMock.Verify(c => c.SetAsync(
            "ts:user:myConv:myUser",
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Delete behavior ────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_RemovesConversationAndUserKeys()
    {
        TurnStateLoader loader = CreateLoader();

        await loader.DeleteAsync("conv1", "user1", CancellationToken.None);

        _cacheMock.Verify(c => c.RemoveAsync("ts:conv:conv1", It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync("ts:user:conv1:user1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NullUserId_RemovesOnlyConversationKey()
    {
        TurnStateLoader loader = CreateLoader();

        await loader.DeleteAsync("conv1", null, CancellationToken.None);

        _cacheMock.Verify(c => c.RemoveAsync("ts:conv:conv1", It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync(It.Is<string>(k => k.StartsWith("ts:user:")), It.IsAny<CancellationToken>()), Times.Never);
    }
}
