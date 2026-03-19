// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.DevTools;

/// <summary>
/// Middleware that intercepts incoming activities and errors, emitting events to DevTools UI clients.
/// </summary>
public class DevToolsMiddleware : ITurnMiddleware
{
    private readonly DevToolsService _service;

    /// <summary>
    /// Creates a new DevToolsMiddleware.
    /// </summary>
    /// <param name="service">The shared DevTools service.</param>
    public DevToolsMiddleware(DevToolsService service)
    {
        _service = service;
    }

    /// <inheritdoc/>
    public async Task OnTurnAsync(BotApplication botApplication, CoreActivity activity, NextTurn nextTurn, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nextTurn);

        // Emit received event before processing
        await _service.EmitReceived(activity, cancellationToken).ConfigureAwait(false);

        try
        {
            await nextTurn(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Emit error event, then re-throw so BotApplication error handling still works
            await _service.EmitError(activity, new { message = ex.Message, stackTrace = ex.StackTrace }, cancellationToken).ConfigureAwait(false);
            throw;
        }
    }
}
