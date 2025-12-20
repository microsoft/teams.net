// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Core.Schema;
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
/// <param name="logger">The <paramref name="logger"/></param>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "<Pending>")]
public class CompatBotAdapter(BotApplication botApplication, ILogger<CompatBotAdapter> logger = default!) : BotAdapter
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
    public override async Task<Microsoft.Bot.Schema.ResourceResponse[]> SendActivitiesAsync(ITurnContext turnContext, Activity[] activities, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(activities);

        Microsoft.Bot.Schema.ResourceResponse[] responses = new Microsoft.Bot.Schema.ResourceResponse[1];
        for (int i = 0; i < activities.Length; i++)
        {
            CoreActivity a = activities[i].FromCompatActivity();

            SendActivityResponse? resp = await botApplication.SendActivityAsync(a, cancellationToken).ConfigureAwait(false);
            if (resp is not null)
            {
                responses[i] = new Microsoft.Bot.Schema.ResourceResponse() { Id = resp.Id };
            }
            else
            {
                logger.LogWarning("Found null ResourceResponse after calling SendActivityAsync");
            }
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
    public override Task<Microsoft.Bot.Schema.ResourceResponse> UpdateActivityAsync(ITurnContext turnContext, Activity activity, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }


}
