// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Plugins.AspNetCore.DevTools;

public class WebSocketCollection : IEnumerable<KeyValuePair<string, WebSocket>>
{
    public int Count => _store.Count;

    protected IDictionary<string, WebSocket> _store;

    public WebSocketCollection()
    {
        _store = new Dictionary<string, WebSocket>();
    }

    public bool Has(params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!_store.ContainsKey(key))
            {
                return false;
            }
        }

        return true;
    }

    public WebSocket? Get(string key)
    {
        if (!_store.ContainsKey(key)) return null;
        return _store[key];
    }

    public WebSocketCollection Add(string key, WebSocket value)
    {
        _store[key] = value;
        return this;
    }

    public WebSocketCollection Remove(params string[] keys)
    {
        foreach (var key in keys)
        {
            _store.Remove(key);
        }

        return this;
    }

    public async Task Emit(IEvent @event, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(@event, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        var buffer = new ArraySegment<byte>(payload, 0, payload.Length);

        foreach (var socket in _store.Values)
        {
            await socket.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationToken);
        }
    }

    public async Task Emit(string key, IEvent @event, CancellationToken cancellationToken = default)
    {
        var socket = Get(key);

        if (socket is null) return;

        var payload = JsonSerializer.SerializeToUtf8Bytes(@event, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        var buffer = new ArraySegment<byte>(payload, 0, payload.Length);
        await socket.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationToken);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<KeyValuePair<string, WebSocket>> GetEnumerator() => _store.GetEnumerator();
}