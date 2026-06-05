// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Apps.State;

/// <summary>
/// Holds the conversation-scoped and user-scoped state for the current turn.
/// </summary>
public sealed class TurnStateContainer
{
    /// <summary>
    /// Gets the conversation-scoped state, shared by all users in the conversation.
    /// Keyed by <c>Conversation.Id</c>.
    /// </summary>
    public ITurnState ConversationState { get; }

    /// <summary>
    /// Gets the user-scoped state, private to each user in each conversation.
    /// Keyed by <c>Conversation.Id</c> + <c>From.Id</c>.
    /// Returns <see langword="null"/> when the activity has no <c>From</c> field.
    /// </summary>
    public ITurnState? UserState { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TurnStateContainer"/> class.
    /// </summary>
    public TurnStateContainer(ITurnState conversationState, ITurnState? userState)
    {
        ConversationState = conversationState;
        UserState = userState;
    }
}
