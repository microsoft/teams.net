// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.Bot.Apps;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Schema;


namespace Microsoft.Teams.Bot.Compat;

/// <summary>
/// Provides a compatibility adapter for processing bot activities and HTTP requests using legacy middleware and bot
/// framework interfaces.
/// </summary>
/// <remarks>Use this adapter to bridge between legacy bot framework middleware and newer bot application models.
/// The adapter allows registration of middleware and error handling delegates, and supports processing HTTP requests
/// and continuing conversations. Thread safety is not guaranteed; instances should not be shared across concurrent
/// requests.</remarks>
public class CompatAdapter : IBotFrameworkHttpAdapter
{
    private readonly TeamsBotApplication _teamsBotApplication;
    private readonly CompatBotAdapter _compatBotAdapter;
    private readonly IServiceProvider _sp;


    /// <summary>
    /// Creates a new instance of the <see cref="CompatAdapter"/> class.
    /// </summary>
    /// <param name="sp"></param>
    /// <param name="keyName"></param>
    public CompatAdapter(IServiceProvider sp, string keyName = "AzureAd")
    {
        _sp = sp;
        _teamsBotApplication = sp.GetRequiredKeyedService<TeamsBotApplication>(keyName);
        _compatBotAdapter = sp.GetRequiredKeyedService<CompatBotAdapter>(keyName);
    }

    /// <summary>
    /// Gets the collection of middleware components configured for the application.
    /// </summary>
    /// <remarks>Use this property to access or inspect the set of middleware that will be invoked during
    /// request processing. The returned collection is read-only and reflects the current middleware pipeline.</remarks>
    public MiddlewareSet MiddlewareSet { get; } = new MiddlewareSet();

    /// <summary>
    /// Gets or sets the error handling callback to be invoked when an exception occurs during a turn.
    /// </summary>
    /// <remarks>Assign a delegate to customize how errors are handled within the bot's turn processing. The
    /// callback receives the current turn context and the exception that was thrown. If not set, unhandled exceptions
    /// may propagate and result in default error behavior. This property is typically used to log errors, send
    /// user-friendly messages, or perform cleanup actions.</remarks>
    public Func<ITurnContext, Exception, Task>? OnTurnError { get; set; }

    /// <summary>
    /// Adds the specified middleware to the adapter's processing pipeline.
    /// </summary>
    /// <param name="middleware">The middleware component to be invoked during request processing. Cannot be null.</param>
    /// <returns>The current <see cref="CompatAdapter"/> instance, enabling method chaining.</returns>
    public CompatAdapter Use(Microsoft.Bot.Builder.IMiddleware middleware)
    {
        MiddlewareSet.Use(middleware);
        return this;
    }

    /// <summary>
    /// Processes an incoming HTTP request and generates an appropriate HTTP response using the provided bot instance.
    /// </summary>
    /// <param name="httpRequest">The incoming HTTP request containing the bot activity. Cannot be null.</param>
    /// <param name="httpResponse">The HTTP response to write results to. Cannot be null.</param>
    /// <param name="bot">The bot instance that will process the activity. Cannot be null.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous processing operation.</returns>
    public async Task ProcessAsync(HttpRequest httpRequest, HttpResponse httpResponse, IBot bot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpRequest);
        ArgumentNullException.ThrowIfNull(httpResponse);
        ArgumentNullException.ThrowIfNull(bot);

        CoreActivity? coreActivity = null;
        _teamsBotApplication.OnActivity = async (activity, cancellationToken1) =>
        {
            coreActivity = activity;
            TurnContext turnContext = new(_compatBotAdapter, activity.ToCompatActivity());
            turnContext.TurnState.Add<Microsoft.Bot.Connector.Authentication.UserTokenClient>(new CompatUserTokenClient(_teamsBotApplication.UserTokenClient));
            CompatConnectorClient connectionClient = new(new CompatConversations(_teamsBotApplication.ConversationClient) { ServiceUrl = activity.ServiceUrl?.ToString() });
            turnContext.TurnState.Add<Microsoft.Bot.Connector.IConnectorClient>(connectionClient);
            turnContext.TurnState.Add<Microsoft.Teams.Bot.Apps.TeamsApiClient>(_teamsBotApplication.TeamsApiClient);
            await MiddlewareSet.ReceiveActivityWithStatusAsync(turnContext, bot.OnTurnAsync, cancellationToken).ConfigureAwait(false);
        };

        try
        {
            await _teamsBotApplication.ProcessAsync(httpRequest.HttpContext, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (OnTurnError != null)
            {
                if (ex is BotHandlerException aex)
                {
                    coreActivity = aex.Activity;
                    using TurnContext turnContext = new(_compatBotAdapter, coreActivity!.ToCompatActivity());
                    await OnTurnError(turnContext, ex).ConfigureAwait(false);
                }
                else
                {
                    throw;
                }
            }
            else
            {
                throw;
            }
        }
    }

    /// <summary>
    /// Continues an existing bot conversation by invoking the specified callback with the provided conversation
    /// reference.
    /// </summary>
    /// <remarks>Use this method to resume a conversation at a specific point, such as in response to an event
    /// or proactive message. The callback is executed within the context of the continued conversation.</remarks>
    /// <param name="botId">The unique identifier of the bot participating in the conversation.</param>
    /// <param name="reference">A reference to the conversation to continue. Must not be null.</param>
    /// <param name="callback">A delegate that handles the bot logic for the continued conversation. The callback receives a turn context and
    /// cancellation token.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ContinueConversationAsync(string botId, ConversationReference reference, BotCallbackHandler callback, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reference);
        ArgumentNullException.ThrowIfNull(callback);

        using TurnContext turnContext = new(_compatBotAdapter, reference.GetContinuationActivity());
        turnContext.TurnState.Add<Microsoft.Bot.Connector.IConnectorClient>(new CompatConnectorClient(new CompatConversations(_teamsBotApplication.ConversationClient) { ServiceUrl = reference.ServiceUrl }));
        turnContext.TurnState.Add<Microsoft.Teams.Bot.Apps.TeamsApiClient>(_teamsBotApplication.TeamsApiClient);
        await callback(turnContext, cancellationToken).ConfigureAwait(false);
    }
}
