// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;


namespace Microsoft.Bot.Core.Compat;

/// <summary>
/// Provides a Bot Framework adapter that enables compatibility between the Bot Framework SDK and a custom bot
/// application implementation.
/// </summary>
/// <remarks>Use this adapter to bridge Bot Framework turn contexts and activities with a custom bot application.
/// This class is intended for scenarios where integration with non-standard bot runtimes or legacy systems is
/// required.</remarks>
/// <param name="botApplication">The bot application instance used to process and send activities within the adapter.</param>
/// <param name="httpContextAccessor" >The HTTP context accessor used to retrieve the current HTTP context.</param>
/// <param name="logger">The <paramref name="logger"/></param>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "<Pending>")]
public class CompatBotAdapter(BotApplication botApplication, IHttpContextAccessor httpContextAccessor = default!, ILogger<CompatBotAdapter> logger = default!) : BotAdapter
{
    /// <summary>
    /// Deletes an activity from the conversation.
    /// </summary>
    /// <param name="turnContext"></param>
    /// <param name="reference"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public override async Task DeleteActivityAsync(ITurnContext turnContext, ConversationReference reference, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        await botApplication.ConversationClient.DeleteActivityAsync(turnContext.Activity.FromCompatActivity(), cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a set of activities to the conversation.
    /// </summary>
    /// <param name="turnContext"></param>
    /// <param name="activities"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async Task<ResourceResponse[]> SendActivitiesAsync(ITurnContext turnContext, Activity[] activities, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(activities);
        ArgumentNullException.ThrowIfNull(turnContext);

        ResourceResponse[] responses = new Microsoft.Bot.Schema.ResourceResponse[activities.Length];

        for (int i = 0; i < activities.Length; i++)
        {
            var activity = activities[i];
            if (activity.Type == "invokeResponse")
            {
                await WriteInvokeResponseToHttpResponseAsync(activity.Value as InvokeResponse, cancellationToken).ConfigureAwait(false);
                return [new ResourceResponse() { Id = null } ];
            }

            SendActivityResponse? resp = await botApplication.SendActivityAsync(activity.FromCompatActivity(), cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Resp from SendActivitiesAsync: {RespId}", resp?.Id);

            responses[i] = new Microsoft.Bot.Schema.ResourceResponse() { Id = resp?.Id };
        }
        return responses;
    }

    /// <summary>
    /// Updates an existing activity in the conversation.
    /// </summary>
    /// <param name="turnContext"></param>
    /// <param name="activity"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public override async Task<ResourceResponse> UpdateActivityAsync(ITurnContext turnContext, Activity activity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(activity);
        var res = await botApplication.ConversationClient.UpdateActivityAsync(
            activity.Conversation.Id,
            activity.Id,
            activity.FromCompatActivity(),
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return new ResourceResponse() { Id = res.Id };
    }

    private async Task WriteInvokeResponseToHttpResponseAsync(InvokeResponse? invokeResponse, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(invokeResponse);
        var response = httpContextAccessor?.HttpContext?.Response;
        ArgumentNullException.ThrowIfNull(response);
        int? status = invokeResponse?.Status;
        //string type = "application/vnd.microsoft.activity.message";
        string? value = invokeResponse?.Body as string;
        response.StatusCode = status ?? 100;
        await response.WriteAsJsonAsync(new
        {
            status,
            value
        },
        cancellationToken).ConfigureAwait(false);
    }

}
