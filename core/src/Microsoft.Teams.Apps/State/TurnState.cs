// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.State;

/// <summary>
/// Per-turn state container exposing the conversation and user scopes. It is loaded at the start of a
/// turn and saved when the turn completes successfully (see <see cref="TurnStateStore"/>).
/// </summary>
public sealed class TurnState
{
    internal TurnState(StateScope conversation, StateScope user, string? conversationKey = null, string? userKey = null)
    {
        Conversation = conversation;
        User = user;
        ConversationKey = conversationKey;
        UserKey = userKey;
    }

    /// <summary>Per-conversation persisted scope.</summary>
    public StateScope Conversation { get; }

    /// <summary>Per-user persisted scope.</summary>
    public StateScope User { get; }

    /// <summary>Storage key for the conversation scope, or null when the scope is non-persisted this turn.</summary>
    internal string? ConversationKey { get; }

    /// <summary>Storage key for the user scope, or null when the scope is non-persisted this turn.</summary>
    internal string? UserKey { get; }

    /// <summary>True once the turn has completed and state has been saved; scope access then throws.</summary>
    public bool IsCompleted { get; private set; }

    /// <summary>Gets a value by path: <c>"conversation.x"</c> or <c>"user.x"</c>.</summary>
    /// <typeparam name="T">The value type to read.</typeparam>
    /// <param name="path">The scoped path.</param>
    public T? GetValue<T>(string path)
    {
        (StateScope scope, string key) = Resolve(path);
        return scope.Get<T>(key);
    }

    /// <summary>Sets a value by path: <c>"conversation.x"</c> or <c>"user.x"</c>.</summary>
    /// <typeparam name="T">The value type to store.</typeparam>
    /// <param name="path">The scoped path.</param>
    /// <param name="value">The value to store.</param>
    public void SetValue<T>(string path, T value)
    {
        (StateScope scope, string key) = Resolve(path);
        scope.Set(key, value);
    }

    /// <summary>
    /// Derives the storage keys for the conversation and user scopes from an inbound
    /// <see cref="CoreActivity"/>. Including the channel id in both keys prevents state from leaking
    /// across channels/tenants. Either key is <see langword="null"/> when the activity lacks the parts
    /// needed to build it; that scope is then non-persisted for the turn.
    /// </summary>
    /// <param name="activity">The incoming activity.</param>
    internal static (string? ConversationKey, string? UserKey) DeriveKeys(CoreActivity activity)
    {
        string? channelId = activity.ChannelId;
        string? conversationId = activity.Conversation?.Id;
        string? fromId = activity.From?.Id;

        string? conversationKey = string.IsNullOrEmpty(channelId) || string.IsNullOrEmpty(conversationId)
            ? null
            : $"{channelId}/conversations/{conversationId}";

        string? userKey = string.IsNullOrEmpty(channelId) || string.IsNullOrEmpty(fromId)
            ? null
            : $"{channelId}/users/{fromId}";

        return (conversationKey, userKey);
    }

    internal void Complete()
    {
        IsCompleted = true;
        Conversation.Complete();
        User.Complete();
    }

    private (StateScope Scope, string Key) Resolve(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        int dot = path.IndexOf('.', StringComparison.Ordinal);
        if (dot < 0)
        {
            throw new ArgumentException(
                $"State path '{path}' must be scope-qualified, e.g. 'conversation.{path}' or 'user.{path}'.",
                nameof(path));
        }

        string scopeName = path[..dot];
        string key = path[(dot + 1)..];
        StateScope scope = scopeName switch
        {
            "conversation" => Conversation,
            "user" => User,
            _ => throw new ArgumentException(
                $"Unknown state scope '{scopeName}' in path '{path}'. Expected 'conversation' or 'user'.",
                nameof(path)),
        };
        return (scope, key);
    }
}
