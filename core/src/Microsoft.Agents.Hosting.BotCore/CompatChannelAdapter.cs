// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Core.Models;
using Microsoft.Agents.Core.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Bot.Core;

namespace Microsoft.Agents.Hosting.BotCore;

/// <summary>
/// Provides a channel adapter that enables compatibility between the Microsoft.Agents framework
/// and the Microsoft.Teams.Bot.Core BotApplication.
/// </summary>
/// <remarks>
/// Use this adapter to bridge Microsoft.Agents turn contexts and activities with BotApplication.
/// This class delegates activity operations to the BotApplication's ConversationClient.
/// </remarks>
/// <remarks>
/// Creates a new instance of the <see cref="CompatChannelAdapter"/> class.
/// </remarks>
/// <param name="botApplication">The BotApplication instance used to process and send activities.</param>
/// <param name="httpContextAccessor">The HTTP context accessor used for writing invoke responses.</param>
/// <param name="logger">The logger instance for recording adapter operations.</param>
public class CompatChannelAdapter(
    BotApplication botApplication,
    IHttpContextAccessor httpContextAccessor,
    ILogger<CompatChannelAdapter> logger) : ChannelAdapter(logger)
{
    private readonly BotApplication _botApplication = botApplication ?? throw new ArgumentNullException(nameof(botApplication));
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    private readonly ILogger<CompatChannelAdapter> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Sends a set of activities to the conversation.
    /// </summary>
    /// <param name="turnContext">The turn context for the conversation.</param>
    /// <param name="activities">An array of activities to send.</param>
    /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
    /// <returns>An array of ResourceResponse objects with the IDs of the sent activities.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "<Pending>")]
    public override async Task<ResourceResponse[]> SendActivitiesAsync(
        ITurnContext turnContext,
        IActivity[] activities,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(activities);
        ArgumentNullException.ThrowIfNull(turnContext);

        var responses = new ResourceResponse[activities.Length];

        for (int i = 0; i < activities.Length; i++)
        {
            var activity = activities[i];

            // Handle Trace activities
            if (activity.Type == ActivityTypes.Trace)
            {
                responses[i] = new ResourceResponse { Id = null };
                continue;
            }

            // Handle InvokeResponse activities
            if (activity.Type == ActivityTypes.InvokeResponse)
            {
                WriteInvokeResponseToHttpResponse(activity.Value as InvokeResponse);
                responses[i] = new ResourceResponse { Id = null };
                continue;
            }

            // Convert Agents Activity to CoreActivity and send
            var coreActivity = activity.ToCoreActivity();
            var resp = await _botApplication.SendActivityAsync(coreActivity, cancellationToken).ConfigureAwait(false);

            _logger.LogDebug("Response from SendActivitiesAsync: {RespId}", resp?.Id);

            responses[i] = new ResourceResponse { Id = resp?.Id };
        }

        return responses;
    }

    /// <summary>
    /// Updates an existing activity in the conversation.
    /// </summary>
    /// <param name="turnContext">The turn context for the conversation.</param>
    /// <param name="activity">The activity with updated content.</param>
    /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
    /// <returns>A ResourceResponse with the ID of the updated activity.</returns>
    public override async Task<ResourceResponse> UpdateActivityAsync(
        ITurnContext turnContext,
        IActivity activity,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(activity);

        var coreActivity = activity.ToCoreActivity();

        var response = await _botApplication.ConversationClient.UpdateActivityAsync(
            activity.Conversation.Id,
            activity.Id,
            coreActivity,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new ResourceResponse { Id = response.Id };
    }

    /// <summary>
    /// Deletes an existing activity from the conversation.
    /// </summary>
    /// <param name="turnContext">The turn context for the conversation.</param>
    /// <param name="reference">The conversation reference identifying the activity to delete.</param>
    /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
    public override async Task DeleteActivityAsync(
        ITurnContext turnContext,
        ConversationReference reference,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        ArgumentNullException.ThrowIfNull(reference);

        // Convert the current activity to CoreActivity for the delete operation
        var coreActivity = turnContext.Activity.ToCoreActivity();

        await _botApplication.ConversationClient.DeleteActivityAsync(
            coreActivity,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes an invoke response directly to the HTTP response.
    /// </summary>
    /// <param name="invokeResponse">The invoke response to write.</param>
    private void WriteInvokeResponseToHttpResponse(InvokeResponse? invokeResponse)
    {
        ArgumentNullException.ThrowIfNull(invokeResponse);

        var response = _httpContextAccessor?.HttpContext?.Response;
        if (response is not null && !response.HasStarted)
        {
            response.StatusCode = invokeResponse.Status ?? 200;

            _logger.LogTrace(
                "Sending Invoke Response with status: {Status}",
                invokeResponse.Status);

            if (invokeResponse.Body is not null)
            {
                using var streamWriter = new StreamWriter(response.BodyWriter.AsStream());
                var json = ProtocolJsonSerializer.ToJson(invokeResponse.Body);
                streamWriter.Write(json);
            }
        }
        else
        {
            _logger.LogWarning(
                "HTTP response is null or has started. Cannot write invoke response. ResponseStarted: {ResponseStarted}",
                response?.HasStarted);
        }
    }
}
