// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Schema;


namespace Microsoft.Teams.Apps.BotBuilder;

/// <summary>
/// Provides a compatibility adapter for processing bot activities and HTTP requests using legacy middleware and bot
/// framework interfaces.
/// </summary>
/// <remarks>Use this adapter to bridge between legacy bot framework middleware and newer bot application models.
/// The adapter allows registration of middleware and error handling delegates, and supports processing HTTP requests
/// and continuing conversations. Thread safety is not guaranteed; instances should not be shared across concurrent
/// requests.</remarks>
public class TeamsBotFrameworkHttpAdapter : TeamsBotAdapter, IBotFrameworkHttpAdapter
{
    private static readonly AsyncLocal<Func<CoreActivity, CancellationToken, Task>?> _activityCallback = new();
    private readonly BotApplication _teamsBotApplication;
    private readonly ILogger? _logger;

    /// <summary>
    /// Creates a new instance of the <see cref="TeamsBotFrameworkHttpAdapter"/> class.
    /// </summary>
    /// <param name="teamsBotApplication">The Teams bot application instance.</param>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    /// <param name="logger">The logger instance.</param>
    public TeamsBotFrameworkHttpAdapter(
        BotApplication teamsBotApplication,
        IHttpContextAccessor? httpContextAccessor = null,
        ILogger? logger = null)
        : base(teamsBotApplication, httpContextAccessor, logger)
    {
        _teamsBotApplication = teamsBotApplication;
        _logger = logger;

        // Set the OnActivity handler once to a dispatcher that delegates to the
        // AsyncLocal callback, isolating each concurrent request's handler.
        _teamsBotApplication.OnActivity = (activity, ct) =>
            _activityCallback.Value?.Invoke(activity, ct) ?? Task.CompletedTask;
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

        TurnContext? turnContext = null;
        _activityCallback.Value = async (activity, ct) =>
        {
            turnContext = new(this, activity.ToBotFrameworkActivity());
            turnContext.TurnState.Add<Microsoft.Bot.Connector.Authentication.UserTokenClient>(new CompatUserTokenClient(_teamsBotApplication.UserTokenClient));
            CompatConnectorClient connectionClient = new(new CompatConversations(_teamsBotApplication.ConversationClient)
            {
                ServiceUrl = activity.ServiceUrl?.ToString(),
                AgenticIdentity = activity.From?.GetAgenticIdentity()
            });
            turnContext.TurnState.Add<Microsoft.Bot.Connector.IConnectorClient>(connectionClient);
            //turnContext.TurnState.Add<Microsoft.Teams.Apps.TeamsApiClient>(_teamsBotApplication.TeamsApiClient); // TODO: review TeamsInfo needs
            await MiddlewareSet.ReceiveActivityWithStatusAsync(turnContext, bot.OnTurnAsync, ct).ConfigureAwait(false);
        };

        try
        {
            await _teamsBotApplication.ProcessAsync(httpRequest.HttpContext, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (OnTurnError != null)
            {
#pragma warning disable CA1508 // turnContext is assigned by the async callback captured in the closure
                if (ex is BotHandlerException aex && turnContext != null)
#pragma warning restore CA1508
                {
                    _logger?.ActivityProcessingErrorDelegating(ex, aex.Activity?.Id);
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
        finally
        {
            _activityCallback.Value = null;
            turnContext?.Dispose();
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
    public async override Task ContinueConversationAsync(string botId, ConversationReference reference, BotCallbackHandler callback, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reference);
        ArgumentNullException.ThrowIfNull(callback);

        using TurnContext turnContext = new(this, reference.GetContinuationActivity());
        turnContext.TurnState.Add<Microsoft.Bot.Connector.Authentication.UserTokenClient>(new CompatUserTokenClient(_teamsBotApplication.UserTokenClient));
        turnContext.TurnState.Add<Microsoft.Bot.Connector.IConnectorClient>(new CompatConnectorClient(new CompatConversations(_teamsBotApplication.ConversationClient) { ServiceUrl = reference.ServiceUrl }));
        await RunPipelineAsync(turnContext, callback, cancellationToken).ConfigureAwait(false);
    }
}
