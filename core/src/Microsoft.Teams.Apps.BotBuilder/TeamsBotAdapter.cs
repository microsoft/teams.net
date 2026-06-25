// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Http;
using Microsoft.Teams.Core.Schema;
using Newtonsoft.Json;


namespace Microsoft.Teams.Apps.BotBuilder;

/// <summary>
/// Provides a Bot Framework adapter that enables compatibility between the Bot Framework SDK and a custom bot
/// application implementation.
/// </summary>
/// <remarks>Use this adapter to bridge Bot Framework turn contexts and activities with a custom bot application.
/// This class is intended for scenarios where integration with non-standard bot runtimes or legacy systems is
/// required.</remarks>
/// <param name="botApplication">The Teams bot application instance.</param>
/// <param name="httpContextAccessor">The HTTP context accessor.</param>
/// <param name="logger">The logger instance.</param>
public class TeamsBotAdapter(
    BotApplication botApplication,
    IHttpContextAccessor? httpContextAccessor = null,
    ILogger? logger = null) : BotAdapter
{
    private readonly JsonSerializerOptions _writeIndentedJsonOptions = new() { WriteIndented = true };
    private readonly BotApplication botApplication = botApplication;
    private readonly IHttpContextAccessor? httpContextAccessor = httpContextAccessor;
    private readonly ILogger? logger = logger;

    /// <summary>
    /// Deletes an activity from the conversation.
    /// </summary>
    /// <param name="turnContext">The turn context containing the activity information. Cannot be null.</param>
    /// <param name="reference">The conversation reference identifying the activity to delete.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    public override async Task DeleteActivityAsync(ITurnContext turnContext, ConversationReference reference, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        ArgumentNullException.ThrowIfNull(reference);

        // Extract values from conversation reference
        string conversationId = reference.Conversation?.Id
            ?? throw new ArgumentException("ConversationReference must contain a valid Conversation.Id", nameof(reference));

        string activityId = reference.ActivityId
            ?? throw new ArgumentException("ConversationReference must contain a valid ActivityId", nameof(reference));

        string serviceUrlString = reference.ServiceUrl
            ?? turnContext.Activity.ServiceUrl
            ?? throw new ArgumentException("ServiceUrl must be provided", nameof(reference));

        Uri serviceUrl = new(serviceUrlString);

        await botApplication.ConversationClient.DeleteActivityAsync(
            conversationId,
            activityId,
            serviceUrl,
            BotRequestProperties.FromInboundActivity(turnContext.Activity?.FromBotFrameworkActivity()),
            customHeaders: null,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a set of activities to the conversation.
    /// </summary>
    /// <param name="turnContext">The turn context for the conversation. Cannot be null.</param>
    /// <param name="activities">An array of activities to send. Cannot be null.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains an array of <see cref="ResourceResponse"/>
    /// objects with the IDs of the sent activities.
    /// </returns>
    public override async Task<ResourceResponse[]> SendActivitiesAsync(ITurnContext turnContext, Activity[] activities, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(activities);
        ArgumentNullException.ThrowIfNull(turnContext);

        ResourceResponse[] responses = new Microsoft.Bot.Schema.ResourceResponse[activities.Length];

        for (int i = 0; i < activities.Length; i++)
        {
            Activity activity = activities[i];

            if (activity.Type == ActivityTypes.Trace)
            {
                return [new ResourceResponse() { Id = null }];
            }

            if (activity.Type == "invokeResponse")
            {
                WriteInvokeResponseToHttpResponse(activity.Value as InvokeResponse);
                return [new ResourceResponse() { Id = null }];
            }

            CoreActivity coreActivity = activity.FromBotFrameworkActivity();

            // Ensure ServiceUrl is set from turn context if not already present
            if (coreActivity.ServiceUrl == null && !string.IsNullOrWhiteSpace(turnContext.Activity.ServiceUrl))
            {
                coreActivity.ServiceUrl = new Uri(turnContext.Activity.ServiceUrl);
            }

            coreActivity.Conversation ??= new Microsoft.Teams.Core.Schema.Conversation(
                turnContext.Activity.Conversation?.Id
                ?? throw new InvalidOperationException("Conversation ID is required to send activities."));
            SendActivityResponse? resp = await botApplication.SendActivityAsync(coreActivity, cancellationToken: cancellationToken).ConfigureAwait(false);

            logger?.SendActivitiesResponse(resp?.Id);

            responses[i] = new Microsoft.Bot.Schema.ResourceResponse() { Id = resp?.Id };
        }
        return responses;
    }

    /// <summary>
    /// Updates an existing activity in the conversation.
    /// </summary>
    /// <param name="turnContext">The turn context for the conversation.</param>
    /// <param name="activity">The activity with updated content. Cannot be null.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a <see cref="ResourceResponse"/>
    /// with the ID of the updated activity.
    /// </returns>
    public override async Task<ResourceResponse> UpdateActivityAsync(ITurnContext turnContext, Activity activity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(turnContext);

        CoreActivity coreActivity = activity.FromBotFrameworkActivity();

        // Ensure ServiceUrl is set from turn context if not already present
        if (coreActivity.ServiceUrl == null && !string.IsNullOrWhiteSpace(turnContext.Activity.ServiceUrl))
        {
            coreActivity.ServiceUrl = new Uri(turnContext.Activity.ServiceUrl);
        }

        UpdateActivityResponse res = await botApplication.ConversationClient.UpdateActivityAsync(
            activity.Conversation.Id,
            activity.Id,
            coreActivity,
            requestProperties: BotRequestProperties.FromInboundActivity(turnContext.Activity?.FromBotFrameworkActivity()),
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return new ResourceResponse() { Id = res.Id };
    }

    private void WriteInvokeResponseToHttpResponse(InvokeResponse? invokeResponse)
    {
        ArgumentNullException.ThrowIfNull(invokeResponse);
        HttpResponse? response = httpContextAccessor?.HttpContext?.Response;
        if (response is not null && !response.HasStarted)
        {
            response.StatusCode = invokeResponse.Status;
            using StreamWriter httpResponseStreamWriter = new(response.BodyWriter.AsStream());
            using JsonTextWriter httpResponseJsonWriter = new(httpResponseStreamWriter);
            if (logger?.IsEnabled(LogLevel.Trace) == true)
            {
                logger.SendingInvokeResponse(System.Text.Json.JsonSerializer.Serialize(invokeResponse.Body, _writeIndentedJsonOptions), invokeResponse.Status);
            }
            if (invokeResponse.Body is not null)
            {
                Microsoft.Bot.Builder.Integration.AspNet.Core.HttpHelper.BotMessageSerializer.Serialize(httpResponseJsonWriter, invokeResponse.Body);
            }
        }
        else
        {
            logger?.CannotWriteInvokeResponse(response?.HasStarted);
        }
    }
}
