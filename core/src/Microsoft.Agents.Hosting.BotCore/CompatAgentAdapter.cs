// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Agents.Builder;
using Microsoft.Agents.Hosting.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Bot.Core;

namespace Microsoft.Agents.Hosting.BotCore;

/// <summary>
/// Provides a compatibility adapter for processing bot activities and HTTP requests
/// using the Microsoft.Agents framework with Microsoft.Teams.Bot.Core BotApplication.
/// </summary>
/// <remarks>
/// Use this adapter to bridge between the BotApplication activity processing
/// and the Microsoft.Agents IAgent interface. The adapter receives HTTP requests,
/// processes them through BotApplication, converts activities, and invokes the agent.
/// </remarks>
/// <remarks>
/// Creates a new instance of the <see cref="CompatAgentAdapter"/> class.
/// </remarks>
/// <param name="botApplication">The BotApplication instance for processing HTTP requests.</param>
/// <param name="compatChannelAdapter">The channel adapter for activity operations.</param>
/// <param name="logger">The logger instance for recording adapter operations.</param>
public class CompatAgentAdapter(
    BotApplication botApplication,
    CompatChannelAdapter compatChannelAdapter,
    ILogger<CompatAgentAdapter> logger) : IAgentHttpAdapter
{
    private readonly BotApplication _botApplication = botApplication ?? throw new ArgumentNullException(nameof(botApplication));
    private readonly CompatChannelAdapter _compatChannelAdapter = compatChannelAdapter ?? throw new ArgumentNullException(nameof(compatChannelAdapter));
    private readonly ILogger<CompatAgentAdapter> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Processes an incoming HTTP request and invokes the agent.
    /// </summary>
    /// <param name="httpRequest">The incoming HTTP request containing the bot activity.</param>
    /// <param name="httpResponse">The HTTP response to write results to.</param>
    /// <param name="agent">The agent instance that will process the activity.</param>
    /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "<Pending>")]
    public async Task ProcessAsync(
        HttpRequest httpRequest,
        HttpResponse httpResponse,
        IAgent agent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpRequest);
        ArgumentNullException.ThrowIfNull(httpResponse);
        ArgumentNullException.ThrowIfNull(agent);

        _botApplication.OnActivity = async (coreActivity, ct) =>
        {
            _logger.LogDebug("Processing activity of type: {ActivityType}", coreActivity.Type);

            // Convert CoreActivity to Microsoft.Agents.Core.Models.Activity
            var agentsActivity = coreActivity.ToAgentsActivity();

            // Create TurnContext with the proper IChannelAdapter and IActivity
            using var turnContext = new TurnContext(_compatChannelAdapter, agentsActivity);

            // Invoke the agent
            await agent.OnTurnAsync(turnContext, ct).ConfigureAwait(false);
        };

        await _botApplication.ProcessAsync(httpRequest.HttpContext, cancellationToken).ConfigureAwait(false);
    }
}
