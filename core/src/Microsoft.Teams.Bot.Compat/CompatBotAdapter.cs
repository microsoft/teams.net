// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Bot.Apps;
using Microsoft.Teams.Bot.Core;
using Newtonsoft.Json;


namespace Microsoft.Teams.Bot.Compat;

/// <summary>
/// Provides a Bot Framework adapter that enables compatibility between the Bot Framework SDK and a custom bot
/// application implementation.
/// </summary>
/// <remarks>Use this adapter to bridge Bot Framework turn contexts and activities with a custom bot application.
/// This class is intended for scenarios where integration with non-standard bot runtimes or legacy systems is
/// required.</remarks>
/// <param name="sp">The service provider used to resolve dependencies.</param>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "<Pending>")]
public class CompatBotAdapter(IServiceProvider sp) : BotAdapter
{
    private readonly JsonSerializerOptions _writeIndentedJsonOptions = new() { WriteIndented = true };
    private readonly TeamsBotApplication botApplication = sp.GetRequiredService<TeamsBotApplication>();
    private readonly IHttpContextAccessor? httpContextAccessor = sp.GetService<IHttpContextAccessor>();
    private readonly ILogger<CompatBotAdapter>? logger = sp.GetService<ILogger<CompatBotAdapter>>();

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
        await botApplication.ConversationClient.DeleteActivityAsync(turnContext.Activity.FromCompatActivity(), cancellationToken: cancellationToken).ConfigureAwait(false);
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

            SendActivityResponse? resp = await botApplication.SendActivityAsync(activity.FromCompatActivity(), cancellationToken).ConfigureAwait(false);

            logger?.LogInformation("Resp from SendActivitiesAsync: {RespId}", resp?.Id);

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
        UpdateActivityResponse res = await botApplication.ConversationClient.UpdateActivityAsync(
            activity.Conversation.Id,
            activity.Id,
            activity.FromCompatActivity(),
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
            logger?.LogTrace("Sending Invoke Response: \n {InvokeResponse} with status: {Status} \n", System.Text.Json.JsonSerializer.Serialize(invokeResponse.Body, _writeIndentedJsonOptions), invokeResponse.Status);
            if (invokeResponse.Body is not null)
            {
                Microsoft.Bot.Builder.Integration.AspNet.Core.HttpHelper.BotMessageSerializer.Serialize(httpResponseJsonWriter, invokeResponse.Body);
            }
        }
        else
        {
            logger?.LogWarning("HTTP response is null or has started. Cannot write invoke response. ResponseStarted: {ResponseStarted}", response?.HasStarted);
        }
    }
}
