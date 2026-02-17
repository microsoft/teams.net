// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Teams.Bot.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Bot.Apps.Api;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Apps.Routing;
using Microsoft.Teams.Bot.Apps.Handlers;

namespace Microsoft.Teams.Bot.Apps;

/// <summary>
/// Teams specific Bot Application
/// </summary>
public class TeamsBotApplication : BotApplication
{
    private readonly TeamsApiClient _teamsApiClient;
    private static TeamsBotApplicationBuilder? _botApplicationBuilder;
    private TeamsApi? _api;

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
    public TeamsApi Api => _api ??= new TeamsApi(
        ConversationClient,
        UserTokenClient,
        _teamsApiClient);

    /// <param name="conversationClient"></param>
    /// <param name="userTokenClient"></param>
    /// <param name="teamsApiClient"></param>
    /// <param name="config"></param>
    /// <param name="httpContextAccessor"></param>
    /// <param name="logger"></param>
    /// <param name="sectionName"></param>
    public TeamsBotApplication(
        ConversationClient conversationClient,
        UserTokenClient userTokenClient,
        TeamsApiClient teamsApiClient,
        IConfiguration config,
        IHttpContextAccessor httpContextAccessor,
        ILogger<TeamsBotApplication> logger,
        string sectionName = "AzureAd")
        : base(conversationClient, userTokenClient, config, logger, sectionName)
    {
        _teamsApiClient = teamsApiClient;
        Router = new Router(logger);
        OnActivity = async (activity, cancellationToken) =>
        {
            TeamsActivity teamsActivity = TeamsActivity.FromActivity(activity);
            Context<TeamsActivity> defaultContext = new(this, teamsActivity);

            if (teamsActivity.Type != TeamsActivityType.Invoke)
            {
                await Router.DispatchAsync(defaultContext, cancellationToken).ConfigureAwait(false);
            }
            else // invokes
            {
                CoreInvokeResponse invokeResponse = await Router.DispatchWithReturnAsync(defaultContext, cancellationToken).ConfigureAwait(false);
                HttpContext? httpContext = httpContextAccessor.HttpContext;
                if (httpContext is not null && invokeResponse is not null)
                {
                    httpContext.Response.StatusCode = invokeResponse.Status;
                    await httpContext.Response.WriteAsJsonAsync(invokeResponse, cancellationToken).ConfigureAwait(false);
                }
            }
        };
    }

    /// <summary>
    /// Creates a new instance of the TeamsBotApplicationBuilder to configure and build a Teams bot application.
    /// </summary>
    /// <returns></returns>
    public static TeamsBotApplicationBuilder CreateBuilder(string[] args)
    {
        _botApplicationBuilder = new TeamsBotApplicationBuilder(args);
        return _botApplicationBuilder;
    }

    /// <summary>
    /// Runs the web application configured by the bot application builder.
    /// </summary>
    /// <remarks>Call CreateBuilder() before invoking this method to ensure the bot application builder is
    /// initialized. This method blocks the calling thread until the web application shuts down.</remarks>
#pragma warning disable CA1822 // Mark members as static
    public void Run()
#pragma warning restore CA1822 // Mark members as static
    {
        ArgumentNullException.ThrowIfNull(_botApplicationBuilder, "BotApplicationBuilder not initialized. Call CreateBuilder() first.");

        _botApplicationBuilder.WebApplication.Run();
    }

}
