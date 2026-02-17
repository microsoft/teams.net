// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core;

namespace Microsoft.Teams.Bot.Apps.Api;

/// <summary>
/// Provides a hierarchical API facade for Teams operations.
/// </summary>
/// <remarks>
/// This class exposes Teams API operations through a structured hierarchy:
/// <list type="bullet">
/// <item><see cref="Conversations"/> - Conversation operations including activities and members</item>
/// <item><see cref="Users"/> - User operations including token management and OAuth sign-in</item>
/// <item><see cref="Teams"/> - Team-specific operations</item>
/// <item><see cref="Meetings"/> - Meeting operations</item>
/// <item><see cref="Batch"/> - Batch messaging operations</item>
/// </list>
/// </remarks>
public class TeamsApi
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TeamsApi"/> class.
    /// </summary>
    /// <param name="conversationClient">The conversation client for conversation operations.</param>
    /// <param name="userTokenClient">The user token client for token operations.</param>
    /// <param name="teamsApiClient">The Teams API client for Teams-specific operations.</param>
    internal TeamsApi(
        ConversationClient conversationClient,
        UserTokenClient userTokenClient,
        TeamsApiClient teamsApiClient)
    {
        Conversations = new ConversationsApi(conversationClient);
        Users = new UsersApi(userTokenClient);
        Teams = new TeamsOperationsApi(teamsApiClient);
        Meetings = new MeetingsApi(teamsApiClient);
        Batch = new BatchApi(teamsApiClient);
    }

    /// <summary>
    /// Gets the conversations API for managing conversation activities and members.
    /// </summary>
    public ConversationsApi Conversations { get; }

    /// <summary>
    /// Gets the users API for user token management and OAuth sign-in.
    /// </summary>
    public UsersApi Users { get; }

    /// <summary>
    /// Gets the Teams-specific operations API.
    /// </summary>
    public TeamsOperationsApi Teams { get; }

    /// <summary>
    /// Gets the meetings API for meeting operations.
    /// </summary>
    public MeetingsApi Meetings { get; }

    /// <summary>
    /// Gets the batch messaging API.
    /// </summary>
    public BatchApi Batch { get; }
}
