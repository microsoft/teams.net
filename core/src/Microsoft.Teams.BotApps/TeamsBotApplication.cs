// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.BotApps.Handlers;
using Microsoft.Teams.BotApps.Schema;

namespace Microsoft.Teams.BotApps;

/// <summary>
/// Teams specific Bot Application
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "<Pending>")]
public class TeamsBotApplication : BotApplication
{

    private static TeamsBotApplicationBuilder? _botApplicationBuilder;

    /// <summary>
    /// Handler for message activities.
    /// </summary>
    public MessageHandler? OnMessage { get; set; }

    /// <summary>
    /// Handler for message reaction activities.
    /// </summary>
    public MessageReactionHandler? OnMessageReaction { get; set; }

    /// <summary>
    /// Handler for installation update activities.
    /// </summary>
    public InstallationUpdateHandler? OnInstallationUpdate { get; set; }

    /// <summary>
    /// Handler for invoke activities.
    /// </summary>
    public InvokeHandler? OnInvoke { get; set; }

    /// <summary>
    /// Handler for conversation update activities.
    /// </summary>
    public ConversationUpdateHandler? OnConversationUpdate { get; set; }
    /// <param name="conversationClient"></param>
    /// <param name="userTokenClient"></param>
    /// <param name="config"></param>
    /// <param name="httpContextAccessor"></param>
    /// <param name="logger"></param>
    /// <param name="sectionName"></param>
    public TeamsBotApplication(ConversationClient conversationClient, UserTokenClient userTokenClient, IConfiguration config, IHttpContextAccessor httpContextAccessor, ILogger<BotApplication> logger, string sectionName = "AzureAd") : base(conversationClient, userTokenClient, config, logger, sectionName)
    {
        OnActivity = async (activity, cancellationToken) =>
        {
            logger.LogInformation("New {Type} activity received.", activity.Type);
            TeamsActivity teamsActivity = TeamsActivity.FromActivity(activity);
            Context context = new(this, teamsActivity);
            if (teamsActivity.Type == TeamsActivityType.Message && OnMessage is not null)
            {
                await OnMessage.Invoke(new MessageArgs(teamsActivity), context, cancellationToken).ConfigureAwait(false);
            }
            if (teamsActivity.Type == TeamsActivityType.InstallationUpdate && OnInstallationUpdate is not null)
            {
                await OnInstallationUpdate.Invoke(new InstallationUpdateArgs(teamsActivity), context, cancellationToken).ConfigureAwait(false);

            }
            if (teamsActivity.Type == TeamsActivityType.MessageReaction && OnMessageReaction is not null)
            {
                await OnMessageReaction.Invoke(new MessageReactionArgs(teamsActivity), context, cancellationToken).ConfigureAwait(false);
            }
            if (teamsActivity.Type == TeamsActivityType.ConversationUpdate && OnConversationUpdate is not null)
            {
                await OnConversationUpdate.Invoke(new ConversationUpdateArgs(teamsActivity), context, cancellationToken).ConfigureAwait(false);
            }
            if (teamsActivity.Type == TeamsActivityType.Invoke && OnInvoke is not null)
            {
                CoreInvokeResponse invokeResponse = await OnInvoke.Invoke(context, cancellationToken).ConfigureAwait(false);
                HttpContext? httpContext = httpContextAccessor.HttpContext;
                if (httpContext is not null)
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
