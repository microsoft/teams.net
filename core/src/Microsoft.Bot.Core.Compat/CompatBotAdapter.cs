// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Core.Schema;
using Microsoft.Bot.Schema;


namespace Microsoft.Bot.Core.Compat;

/// <summary>
/// Provides a Bot Framework adapter that enables compatibility between the Bot Framework SDK and a custom bot
/// application implementation.
/// </summary>
/// <remarks>Use this adapter to bridge Bot Framework turn contexts and activities with a custom bot application.
/// This class is intended for scenarios where integration with non-standard bot runtimes or legacy systems is
/// required.</remarks>
/// <param name="botApplication">The bot application instance used to process and send activities within the adapter.</param>
public class CompatBotAdapter(BotApplication botApplication) : BotAdapter
{
    /// <summary>
    /// Deletes an activity from the conversation.
    /// </summary>
    /// <param name="turnContext"></param>
    /// <param name="reference"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public override Task DeleteActivityAsync(ITurnContext turnContext, ConversationReference reference, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
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

        ResourceResponse[] responses = new ResourceResponse[1];
        for (int i = 0; i < activities.Length; i++)
        {
            CoreActivity a = activities[i].FromCompatActivity();

            string resp = await botApplication.SendActivityAsync(a, cancellationToken).ConfigureAwait(false);
            responses[i] = new ResourceResponse(id: resp);
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
    public override Task<ResourceResponse> UpdateActivityAsync(ITurnContext turnContext, Activity activity, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }


}
