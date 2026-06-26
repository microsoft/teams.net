// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Apps.State;

/// <summary>
/// Holds the conversation-scoped and user-scoped state for the current turn.
/// </summary>
public sealed class TurnStateContainer
{
    private Func<CancellationToken, Task>? _deleteDelegate;

    /// <summary>
    /// Gets the conversation-scoped state, shared by all users in the conversation.
    /// Keyed by <c>Conversation.Id</c>.
    /// </summary>
    public TurnState ConversationState { get; }

    /// <summary>
    /// Gets the user-scoped state, private to each user in each conversation.
    /// Keyed by <c>Conversation.Id</c> + <c>From.Id</c>.
    /// Returns <see langword="null"/> when the activity has no <c>From</c> field.
    /// </summary>
    public TurnState? UserState { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TurnStateContainer"/> class.
    /// </summary>
    public TurnStateContainer(TurnState conversationState, TurnState? userState)
    {
        ArgumentNullException.ThrowIfNull(conversationState);
        ConversationState = conversationState;
        UserState = userState;
    }

    /// <summary>
    /// Sets the delegate used to delete state from the backing store.
    /// Called by the framework after loading state.
    /// </summary>
    internal void SetDeleteDelegate(Func<CancellationToken, Task> deleteDelegate)
    {
        _deleteDelegate = deleteDelegate;
    }

    /// <summary>
    /// Deletes conversation and user state from the backing store.
    /// The in-memory state remains accessible for the rest of the turn
    /// but will not be persisted at end-of-turn unless new values are written.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        if (_deleteDelegate is null)
        {
            throw new InvalidOperationException(
                "State deletion is not available. Call UseState() during service registration.");
        }

        await _deleteDelegate(cancellationToken).ConfigureAwait(false);

        // Clear dirty flags so end-of-turn save doesn't re-persist the deleted state.
        ConversationState.IsDirty = false;
        if (UserState is not null)
        {
            UserState.IsDirty = false;
        }
    }
}
