// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.State;

namespace Microsoft.Teams.Apps.UnitTests.State;

public class TurnStateContainerTests
{
    [Fact]
    public async Task DeleteAsync_CallsDelegate()
    {
        bool delegateCalled = false;
        TurnStateContainer container = CreateContainer();
        container.SetDeleteDelegate(_ =>
        {
            delegateCalled = true;
            return Task.CompletedTask;
        });

        await container.DeleteAsync();

        Assert.True(delegateCalled);
    }

    [Fact]
    public async Task DeleteAsync_ClearsDirtyOnConversationState()
    {
        TurnStateContainer container = CreateContainer();
        container.SetDeleteDelegate(_ => Task.CompletedTask);
        container.ConversationState.Set("key", "value");
        Assert.True(container.ConversationState.IsDirty);

        await container.DeleteAsync();

        Assert.False(container.ConversationState.IsDirty);
    }

    [Fact]
    public async Task DeleteAsync_ClearsDirtyOnUserState()
    {
        TurnStateContainer container = CreateContainer();
        container.SetDeleteDelegate(_ => Task.CompletedTask);
        container.UserState!.Set("key", "value");
        Assert.True(container.UserState.IsDirty);

        await container.DeleteAsync();

        Assert.False(container.UserState.IsDirty);
    }

    [Fact]
    public async Task DeleteAsync_NullUserState_DoesNotThrow()
    {
        TurnStateContainer container = new(new TurnState(), userState: null);
        container.SetDeleteDelegate(_ => Task.CompletedTask);
        container.ConversationState.Set("key", "value");

        await container.DeleteAsync();

        Assert.False(container.ConversationState.IsDirty);
    }

    [Fact]
    public async Task DeleteAsync_WithoutDelegate_Throws()
    {
        TurnStateContainer container = CreateContainer();

        await Assert.ThrowsAsync<InvalidOperationException>(() => container.DeleteAsync());
    }

    [Fact]
    public async Task DeleteAsync_SubsequentMutation_SetsDirtyAgain()
    {
        TurnStateContainer container = CreateContainer();
        container.SetDeleteDelegate(_ => Task.CompletedTask);
        container.ConversationState.Set("key", "value");

        await container.DeleteAsync();
        Assert.False(container.ConversationState.IsDirty);

        container.ConversationState.Set("new-key", "new-value");
        Assert.True(container.ConversationState.IsDirty);
    }

    [Fact]
    public async Task DeleteAsync_ClearsInMemoryConversationState()
    {
        TurnStateContainer container = CreateContainer();
        container.SetDeleteDelegate(_ => Task.CompletedTask);
        container.ConversationState.Set("key", "value");

        await container.DeleteAsync();

        Assert.False(container.ConversationState.ContainsKey("key"));
    }

    [Fact]
    public async Task DeleteAsync_ClearsInMemoryUserState()
    {
        TurnStateContainer container = CreateContainer();
        container.SetDeleteDelegate(_ => Task.CompletedTask);
        container.UserState!.Set("name", "Ada");

        await container.DeleteAsync();

        Assert.False(container.UserState.ContainsKey("name"));
    }

    [Fact]
    public async Task DeleteAsync_PassesCancellationToken()
    {
        CancellationToken captured = default;
        TurnStateContainer container = CreateContainer();
        container.SetDeleteDelegate(ct =>
        {
            captured = ct;
            return Task.CompletedTask;
        });

        using CancellationTokenSource cts = new();
        await container.DeleteAsync(cts.Token);

        Assert.Equal(cts.Token, captured);
    }

    private static TurnStateContainer CreateContainer()
    {
        return new TurnStateContainer(new TurnState(), new TurnState());
    }
}
