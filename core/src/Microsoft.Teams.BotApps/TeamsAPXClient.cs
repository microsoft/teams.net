// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Core;
using Microsoft.Extensions.Logging;

namespace Microsoft.Teams.BotApps;

/// <summary>
/// Provides methods for interacting with the TeamsAPX service.
/// </summary>
public class TeamsAPXClient(HttpClient httpClient, ILogger<UserTokenClient> logger)
{
    /// <summary>
    /// Sends a notification to the specified conversation using the TeamsAPX service.
    /// </summary>
    /// <param name="conversationId">The unique identifier of the conversation to which the notification will be sent. Cannot be null or empty.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous notification operation.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "<Pending>")]
    public async Task NotifyActivityAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Notifying activity for conversation ID: {ConversationId}", conversationId);
        httpClient.BaseAddress = new Uri("https://teamsapx.microsoft.com/") ;
        // Implementation for notifying activity via TeamsAPX service.
        await Task.CompletedTask.ConfigureAwait(false);
    }
}
