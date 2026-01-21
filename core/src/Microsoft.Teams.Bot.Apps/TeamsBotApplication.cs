// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Teams.Bot.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Apps.Routing;
using Microsoft.Teams.Bot.Apps.Schema.MessageActivities;

namespace Microsoft.Teams.Bot.Apps;

/// <summary>
/// Teams specific Bot Application
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "<Pending>")]
public class TeamsBotApplication : BotApplication
{
    private readonly TeamsApiClient _teamsAPXClient;
    private static TeamsBotApplicationBuilder? _botApplicationBuilder;
    internal static Router Router = new Router();
    
    /// <summary>
    /// Gets the client used to interact with the TeamsAPX service.
    /// </summary>
    public TeamsApiClient TeamsAPXClient => _teamsAPXClient;


    /// <param name="conversationClient"></param>
    /// <param name="userTokenClient"></param>
    /// <param name="teamsAPXClient"></param>
    /// <param name="config"></param>
    /// <param name="httpContextAccessor"></param>
    /// <param name="logger"></param>
    /// <param name="sectionName"></param>
    public TeamsBotApplication(
        ConversationClient conversationClient,
        UserTokenClient userTokenClient,
        TeamsApiClient teamsAPXClient,
        IConfiguration config,
        IHttpContextAccessor httpContextAccessor,
        ILogger<BotApplication> logger,
        string sectionName = "AzureAd")
        : base(conversationClient, userTokenClient, config, logger, sectionName)
    {
        _teamsAPXClient = teamsAPXClient;
        OnActivity = async (activity, cancellationToken) =>
        {
            logger.LogInformation("New {Type} activity received.", activity.Type);
            TeamsActivity teamsActivity = TeamsActivity.FromActivity(activity);
            Context<TeamsActivity> defaultContext = new(this, teamsActivity);
            await Router.DispatchAsync(defaultContext).ConfigureAwait(false);
        };
    }

    /// <summary>
    /// Creates a new instance of the TeamsBotApplicationBuilder to configure and build a Teams bot application.
    /// </summary>
    /// <returns></returns>
    public static TeamsBotApplicationBuilder CreateBuilder()
    {
        _botApplicationBuilder = new TeamsBotApplicationBuilder();
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
