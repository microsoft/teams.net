// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.Events;
using Microsoft.Teams.Apps.Plugins;
using Microsoft.Teams.Common.Http;

namespace Microsoft.Teams.Apps;

public partial class App
{
    internal EventEmitter Events = new();

    protected async Task OnErrorEvent(IPlugin sender, ErrorEvent @event, CancellationToken cancellationToken = default)
    {
        cancellationToken = @event.Context?.CancellationToken ?? cancellationToken;
        Logger.Error(@event.Exception);

        if (@event.Exception is HttpException ex)
        {
            Logger.Error(ex.Request?.RequestUri?.ToString());

            if (ex.Request?.Content is not null)
            {
                var content = await ex.Request.Content.ReadAsStringAsync();
                Logger.Error(content);
            }
        }

        foreach (var plugin in Plugins)
        {
            if (ReferenceEquals(sender, plugin)) continue;
            await plugin.OnError(this, sender, @event, cancellationToken);
        }
    }

    protected Task<Response> OnActivityEvent(ISenderPlugin sender, ActivityEvent @event, CancellationToken cancellationToken = default)
    {
        Logger.Debug(EventType.Activity);
        return Process(sender, @event, cancellationToken);
    }

    protected async Task OnActivitySentEvent(ISenderPlugin sender, ActivitySentEvent @event, CancellationToken cancellationToken = default)
    {
        Logger.Debug(EventType.ActivitySent);
        Logger.Debug($"[DEBUG] OnActivitySentEvent - Sender: {sender.GetType().Name} (Hash: {sender.GetHashCode()})");
        Logger.Debug($"[DEBUG] OnActivitySentEvent - Plugins count: {Plugins.Count}");

        foreach (var plugin in Plugins)
        {
            Logger.Debug($"[DEBUG] OnActivitySentEvent - Checking plugin: {plugin.GetType().Name} (Hash: {plugin.GetHashCode()})");
            Logger.Debug($"[DEBUG] OnActivitySentEvent - sender.Equals(plugin): {sender.Equals(plugin)}");
            Logger.Debug($"[DEBUG] OnActivitySentEvent - ReferenceEquals(sender, plugin): {ReferenceEquals(sender, plugin)}");

            if (ReferenceEquals(sender, plugin))
            {
                Logger.Debug($"[DEBUG] OnActivitySentEvent - Skipping plugin: {plugin.GetType().Name}");
                continue;
            }

            Logger.Debug($"[DEBUG] OnActivitySentEvent - Calling OnActivitySent for plugin: {plugin.GetType().Name}");
            await plugin.OnActivitySent(this, sender, @event, cancellationToken);
        }
    }

    protected async Task OnActivityResponseEvent(ISenderPlugin sender, ActivityResponseEvent @event, CancellationToken cancellationToken = default)
    {
        Logger.Debug(EventType.ActivityResponse);

        foreach (var plugin in Plugins)
        {
            if (ReferenceEquals(sender, plugin)) continue;
            await plugin.OnActivityResponse(this, sender, @event, cancellationToken);
        }
    }
}