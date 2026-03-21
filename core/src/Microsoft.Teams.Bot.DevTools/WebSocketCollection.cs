// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Teams.Bot.DevTools.Events;

namespace Microsoft.Teams.Bot.DevTools;

/// <summary>
/// Manages a collection of WebSocket connections and broadcasts events to them.
/// </summary>
public class WebSocketCollection : IEnumerable<KeyValuePair<string, WebSocket>>
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly Dictionary<string, WebSocket> _store = [];

    /// <summary>
    /// Gets the number of connected sockets.
    /// </summary>
    public int Count => _store.Count;

    /// <summary>
    /// Returns true if all specified keys exist in the collection.
    /// </summary>
    public bool Has(params string[] keys)
    {
        ArgumentNullException.ThrowIfNull(keys);

        foreach (var key in keys)
        {
            if (!_store.ContainsKey(key))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Gets the WebSocket for the given key, or null if not found.
    /// </summary>
    public WebSocket? Get(string key)
    {
        return _store.TryGetValue(key, out var socket) ? socket : null;
    }

    /// <summary>
    /// Adds or replaces a WebSocket connection by key.
    /// </summary>
    public WebSocketCollection Add(string key, WebSocket value)
    {
        _store[key] = value;
        return this;
    }

    /// <summary>
    /// Removes WebSocket connections by key.
    /// </summary>
    public WebSocketCollection Remove(params string[] keys)
    {
        ArgumentNullException.ThrowIfNull(keys);

        foreach (var key in keys)
        {
            _store.Remove(key);
        }

        return this;
    }

    /// <summary>
    /// Broadcasts an event to all connected WebSocket clients.
    /// </summary>
    public async Task Emit(IDevToolsEvent @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        var payload = JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), SerializerOptions);
        var buffer = new ArraySegment<byte>(payload, 0, payload.Length);

        foreach (var socket in _store.Values)
        {
            await socket.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Sends an event to a single connected WebSocket client by id.
    /// </summary>
    public async Task Emit(string key, IDevToolsEvent @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        var socket = Get(key);
        if (socket is null) return;

        var payload = JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), SerializerOptions);
        var buffer = new ArraySegment<byte>(payload, 0, payload.Length);
        await socket.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<string, WebSocket>> GetEnumerator() => _store.GetEnumerator();
}
