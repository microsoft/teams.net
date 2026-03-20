// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Bot.Apps.Api;
using Microsoft.Teams.Bot.Apps.Routing;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Hosting;

namespace Microsoft.Teams.Bot.Apps;

/// <summary>
/// Teams specific Bot Application
/// </summary>
public class TeamsBotApplication : BotApplication
{
    private readonly TeamsApiClient _teamsApiClient;

    /// <summary>
    /// Gets the router for dispatching Teams activities to registered routes.
    /// </summary>
    internal Router Router { get; }

    /// <summary>
    /// Gets the client used to interact with the Teams API service.
    /// </summary>
    public TeamsApiClient TeamsApiClient => _teamsApiClient;
    /// <summary>
    /// Gets the hierarchical API facade for Teams operations.
    /// </summary>
    /// <remarks>
    /// This property provides a structured API for accessing Teams operations through a hierarchy:
    /// <list type="bullet">
    /// <item><c>Api.Conversations.Activities</c> - Activity operations (send, update, delete)</item>
    /// <item><c>Api.Conversations.Members</c> - Member operations (get, delete)</item>
    /// <item><c>Api.Users.Token</c> - User token operations (OAuth SSO, sign-in resources)</item>
    /// <item><c>Api.Teams</c> - Team operations (get details, channels)</item>
    /// <item><c>Api.Meetings</c> - Meeting operations (get info, participant, notifications)</item>
    /// <item><c>Api.Batch</c> - Batch messaging operations</item>
    /// </list>
    /// </remarks>
    public TeamsApi Api { get; }

    /// <param name="conversationClient"></param>
    /// <param name="userTokenClient"></param>
    /// <param name="teamsApiClient"></param>
    /// <param name="httpContextAccessor"></param>
    /// <param name="logger"></param>
    /// <param name="options">Options containing the application (client) ID, used for logging and diagnostics. Defaults to an empty instance if not provided.</param>
    public TeamsBotApplication(
        ConversationClient conversationClient,
        UserTokenClient userTokenClient,
        TeamsApiClient teamsApiClient,
        IHttpContextAccessor httpContextAccessor,
        ILogger<TeamsBotApplication> logger,
        BotApplicationOptions? options = null)
        : base(conversationClient, userTokenClient, logger, options)
    {
        _teamsApiClient = teamsApiClient;
        Api = new TeamsApi(conversationClient, userTokenClient, teamsApiClient);
        Router = new Router(logger);
        OnActivity = async (activity, cancellationToken) =>
        {
            logger.LogInformation("OnActivity invoked for activity: Id={Id}", activity.Id);
            TeamsActivity teamsActivity = TeamsActivity.FromActivity(activity);
            Context<TeamsActivity> defaultContext = new(this, teamsActivity);

            if (teamsActivity.Type != TeamsActivityType.Invoke)
            {
                await Router.DispatchAsync(defaultContext, cancellationToken).ConfigureAwait(false);
            }
            else // invokes
            {
                InvokeResponse invokeResponse = await Router.DispatchWithReturnAsync(defaultContext, cancellationToken).ConfigureAwait(false);
                HttpContext? httpContext = httpContextAccessor.HttpContext;
                if (httpContext is not null && invokeResponse is not null)
                {
                    httpContext.Response.StatusCode = invokeResponse.Status;
                    logger.LogTrace("Sending invoke response with status {Status} and Body {Body}", invokeResponse.Status, invokeResponse.Body);
                    if (invokeResponse.Body is not null)
                        await httpContext.Response.WriteAsJsonAsync(invokeResponse.Body, cancellationToken).ConfigureAwait(false);
                }
            }
        };
    }
}
