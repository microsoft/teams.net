// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core;

namespace Microsoft.Teams.Bot.Apps.Api;

/// <summary>
/// Provides conversation-related operations.
/// </summary>
/// <remarks>
/// This class serves as a container for conversation-specific sub-APIs:
/// <list type="bullet">
/// <item><see cref="Activities"/> - Activity operations (send, update, delete, history)</item>
/// <item><see cref="Members"/> - Member operations (get, delete)</item>
/// <item><see cref="Reactions"/> - Reaction operations (add, delete)</item>
/// </list>
/// </remarks>
public class ConversationsApi
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationsApi"/> class.
    /// </summary>
    /// <param name="conversationClient">The conversation client for conversation operations.</param>
    internal ConversationsApi(ConversationClient conversationClient)
    {
        Activities = new ActivitiesApi(conversationClient);
        Members = new MembersApi(conversationClient);
        Reactions = new ReactionsApi(conversationClient);
    }

    /// <summary>
    /// Gets the activities API for sending, updating, and deleting activities.
    /// </summary>
    public ActivitiesApi Activities { get; }

    /// <summary>
    /// Gets the members API for managing conversation members.
    /// </summary>
    public MembersApi Members { get; }

    /// <summary>
    /// Gets the reactions API for adding and removing reactions on activities.
    /// </summary>
    public ReactionsApi Reactions { get; }
}
