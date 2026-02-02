// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Teams.Bot.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Apps.Routing;
using Microsoft.Teams.Bot.Apps.Schema.MessageActivities;
using Microsoft.Teams.Bot.Apps.Handlers;

namespace Microsoft.Teams.Bot.Apps;

/// <summary>
/// Teams specific Bot Application
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "<Pending>")]
public class TeamsBotApplication : BotApplication
{
    private readonly TeamsApiClient _teamsApiClient;
    internal Router Router { get; } = new();
    
    /// <summary>
    /// Gets the client used to interact with the Teams API service.
    /// </summary>
    public TeamsApiClient TeamsApiClient => _teamsApiClient;


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
        ILogger<BotApplication> logger,
        string sectionName = "AzureAd")
        : base(conversationClient, userTokenClient, config, logger, sectionName)
    {
        _teamsApiClient = teamsApiClient;
        OnActivity = async (activity, cancellationToken) =>
        {
            logger.LogInformation("New {Type} activity received.", activity.Type);
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
    /// <returns>A new <see cref="TeamsBotApplicationBuilder"/> instance.</returns>
    public static TeamsBotApplicationBuilder CreateBuilder() => new();

    /// <summary>
    /// Runs the web application configured by the bot application builder.
    /// </summary>
    /// <param name="builder">The bot application builder containing the configured web application.</param>
    /// <remarks>This method blocks the calling thread until the web application shuts down.</remarks>
    public static void Run(TeamsBotApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.WebApplication.Run();
    }

    /// <summary>
    /// Runs the web application asynchronously.
    /// </summary>
    /// <param name="builder">The bot application builder containing the configured web application.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static Task RunAsync(TeamsBotApplicationBuilder builder, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(builder);
        // Use IHost.RunAsync extension which accepts CancellationToken
        return HostingAbstractionsHostExtensions.RunAsync(builder.WebApplication, cancellationToken);
    }

}
