// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.Schema;

namespace Microsoft.Teams.Apps.State;

/// <summary>
/// Per-turn state container exposing the conversation, user, and temp scopes. It is loaded at the
/// start of a turn and saved when the turn completes successfully (see <see cref="StateMiddleware"/>).
/// </summary>
public sealed class TurnState
{
    private static readonly AsyncLocal<TurnState?> CurrentTurnState = new();

    internal TurnState(StateScope conversation, StateScope user, StateScope temp)
    {
        Conversation = conversation;
        User = user;
        Temp = temp;
    }

    /// <summary>
    /// The ambient <see cref="TurnState"/> for the current turn, published by <see cref="StateMiddleware"/>.
    /// <see langword="null"/> outside a turn or when state middleware is not registered.
    /// </summary>
    public static TurnState? Current => CurrentTurnState.Value;

    /// <summary>Per-conversation persisted scope.</summary>
    public StateScope Conversation { get; }

    /// <summary>Per-user persisted scope.</summary>
    public StateScope User { get; }

    /// <summary>Per-turn, non-persisted scratch scope.</summary>
    public StateScope Temp { get; }

    /// <summary>True once the turn has completed and state has been saved; scope access then throws.</summary>
    public bool IsCompleted { get; private set; }

    /// <summary>
    /// Gets a value by path: <c>"conversation.x"</c>, <c>"user.x"</c>, <c>"temp.x"</c>, or a bare
    /// <c>"x"</c> (which defaults to the temp scope).
    /// </summary>
    /// <typeparam name="T">The value type to read.</typeparam>
    /// <param name="path">The scoped path.</param>
    public T? GetValue<T>(string path)
    {
        (StateScope scope, string key) = Resolve(path);
        return scope.Get<T>(key);
    }

    /// <summary>
    /// Sets a value by path: <c>"conversation.x"</c>, <c>"user.x"</c>, <c>"temp.x"</c>, or a bare
    /// <c>"x"</c> (which defaults to the temp scope).
    /// </summary>
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
    /// <see cref="TeamsActivity"/>. Including the channel id in both keys prevents state from leaking
    /// across channels/tenants. Either key is <see langword="null"/> when the activity lacks the parts
    /// needed to build it; that scope is then non-persisted for the turn.
    /// </summary>
    /// <param name="activity">The incoming activity.</param>
    internal static (string? ConversationKey, string? UserKey) DeriveKeys(TeamsActivity activity)
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

    internal static void SetCurrent(TurnState? turnState) => CurrentTurnState.Value = turnState;

    internal void Complete()
    {
        IsCompleted = true;
        Conversation.Complete();
        User.Complete();
        Temp.Complete();
    }

    private (StateScope Scope, string Key) Resolve(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        int dot = path.IndexOf('.', StringComparison.Ordinal);
        if (dot < 0)
        {
            return (Temp, path);
        }

        string scopeName = path[..dot];
        string key = path[(dot + 1)..];
        StateScope scope = scopeName switch
        {
            "conversation" => Conversation,
            "user" => User,
            "temp" => Temp,
            _ => throw new ArgumentException(
                $"Unknown state scope '{scopeName}' in path '{path}'. Expected 'conversation', 'user', or 'temp'.",
                nameof(path)),
        };
        return (scope, key);
    }
}
