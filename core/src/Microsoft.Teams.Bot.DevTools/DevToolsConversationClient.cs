// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.DevTools;

using CustomHeaders = Dictionary<string, string>;

/// <summary>
/// Decorator around <see cref="ConversationClient"/> that emits "sent" events to DevTools UI clients
/// whenever an activity is sent.
/// </summary>
public class DevToolsConversationClient : ConversationClient
{
    private readonly DevToolsService _service;

    /// <summary>
    /// Creates a new DevToolsConversationClient.
    /// </summary>
    /// <param name="httpClient">The HTTP client for sending activities.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="service">The shared DevTools service for emitting events.</param>
    public DevToolsConversationClient(HttpClient httpClient, ILogger<ConversationClient> logger, DevToolsService service)
        : base(httpClient, logger)
    {
        _service = service;
    }

    /// <inheritdoc/>
    public override async Task<SendActivityResponse> SendActivityAsync(CoreActivity activity, CustomHeaders? customHeaders = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        var response = await base.SendActivityAsync(activity, customHeaders, cancellationToken).ConfigureAwait(false);

        // Ensure activity has an ID so the DevTools UI can distinguish messages
        activity.Id ??= response.Id ?? Guid.NewGuid().ToString();

        // Emit sent event after successful send
        await _service.EmitSent(activity, cancellationToken).ConfigureAwait(false);

        return response;
    }
}
