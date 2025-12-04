// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.Teams.Apps.Events;
using Microsoft.Teams.Apps.Plugins;

namespace Microsoft.Teams.Apps;

public partial class App
{
    internal EventEmitter Events = new();

    protected async Task OnErrorEvent(IPlugin sender, ErrorEvent @event, CancellationToken cancellationToken = default)
    {
        cancellationToken = @event.Context?.CancellationToken ?? cancellationToken;
        Logger?.LogError(@event.Exception, @event.Exception.Message);

        // TODO: review events errors
        //if (@event.Exception is HttpException ex)
        //{
        //    Logger.Error(ex.Request?.RequestUri?.ToString());

        //    if (ex.Request?.Content is not null)
        //    {
        //        var content = await ex.Request.Content.ReadAsStringAsync();
        //        Logger.Error(content);
        //    }
        //}

        foreach (var plugin in Plugins!)
        {
            if (sender.Equals(plugin)) continue;
            await plugin.OnError(this, sender, @event, cancellationToken);
        }
    }

    protected Task<Response> OnActivityEvent(ISenderPlugin sender, ActivityEvent @event, CancellationToken cancellationToken = default)
    {
        Logger?.LogDebug(EventType.Activity);
        return Process(sender, @event, cancellationToken);
    }

    protected async Task OnActivitySentEvent(ISenderPlugin sender, ActivitySentEvent @event, CancellationToken cancellationToken = default)
    {
        Logger?.LogDebug(EventType.ActivitySent);

        foreach (var plugin in Plugins!)
        {
            if (sender.Equals(plugin)) continue;
            await plugin.OnActivitySent(this, sender, @event, cancellationToken);
        }
    }

    protected async Task OnActivityResponseEvent(ISenderPlugin sender, ActivityResponseEvent @event, CancellationToken cancellationToken = default)
    {
        Logger?.LogDebug(EventType.ActivityResponse);

        foreach (var plugin in Plugins!)
        {
            if (sender.Equals(plugin)) continue;
            await plugin.OnActivityResponse(this, sender, @event, cancellationToken);
        }
    }
}