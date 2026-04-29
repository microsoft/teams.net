// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Teams.Core.Hosting;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Core;

/// <summary>
/// Represents a bot application.
/// </summary>
public class BotApplication
{
    private readonly ILogger<BotApplication> _logger;
    private readonly ConversationClient? _conversationClient;
    private readonly UserTokenClient? _userTokenClient;
    private readonly TimeSpan _processActivityTimeout = TimeSpan.FromMinutes(5);
    internal TurnMiddleware MiddleWare { get; }

    /// <summary>
    /// Creates a default instance, primarily for testing purposes. The ConversationClient and UserTokenClient properties will not be initialized
    /// </summary>
    protected BotApplication()
    {
        _logger = NullLogger<BotApplication>.Instance;
        AppId = string.Empty;
        MiddleWare = new TurnMiddleware();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BotApplication"/> class with the specified conversation client, user token client,
    /// logger, and optional application options.
    /// </summary>
    /// <param name="conversationClient">The client used to manage and interact with conversations for the bot.</param>
    /// <param name="userTokenClient">The client used to manage user tokens for authentication.</param>
    /// <param name="logger">The logger used to record operational and diagnostic information for the bot application.</param>
    /// <param name="options">Options containing the application (client) ID, used for logging and diagnostics. Defaults to an empty instance if not provided.</param>
    public BotApplication(ConversationClient conversationClient, UserTokenClient userTokenClient, ILogger<BotApplication> logger, BotApplicationOptions? options = null)
    {
        options ??= new();
        _logger = logger;
        AppId = options.AppId;
        MiddleWare = new TurnMiddleware();
        MiddleWare.SetLogger(logger);
        _conversationClient = conversationClient;
        _userTokenClient = userTokenClient;
        _processActivityTimeout = options.ProcessActivityTimeout;
        logger.BotStarted(GetType().Name, options.AppId, Version);
    }


    /// <summary>
    /// Gets the application (client) ID configured for this bot.
    /// </summary>
    public string AppId { get; }

    /// <summary>
    /// Gets the client used to manage and interact with conversations.
    /// </summary>
    /// <remarks>Accessing this property before the client is initialized will result in an exception. Ensure
    /// that the client is properly configured before use.</remarks>
    public ConversationClient ConversationClient => _conversationClient ?? throw new InvalidOperationException("ConversationClient not initialized");

    /// <summary>
    /// Gets the client used to manage user tokens for authentication.
    /// </summary>
    /// <remarks>Accessing this property before the client is initialized will result in an exception. Ensure
    /// that the client is properly configured before use.</remarks>
    public UserTokenClient UserTokenClient => _userTokenClient ?? throw new InvalidOperationException("UserTokenClient not registered");

    /// <summary>
    /// Gets or sets the delegate that is invoked to handle an incoming activity asynchronously.
    /// </summary>
    /// <remarks>Assign a delegate to process activities as they are received. The delegate should accept an
    /// <see cref="CoreActivity"/> and a <see cref="CancellationToken"/>, and return a <see cref="Task"/> representing the
    /// asynchronous operation. If <see langword="null"/>, incoming activities will not be handled.</remarks>
    public virtual Func<CoreActivity, CancellationToken, Task>? OnActivity { get; set; }

    /// <summary>
    /// Processes an incoming HTTP request containing a bot activity.
    /// </summary>
    /// <param name="httpContext">The HTTP context containing the incoming bot activity request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation. Note: a dedicated timeout is used internally instead of the HTTP request token.</param>
    /// <returns>A task that represents the asynchronous activity processing operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the request body cannot be deserialized into a valid activity.</exception>
    /// <exception cref="BotHandlerException">Thrown if an error occurs while processing the activity.</exception>
    public virtual async Task ProcessAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(_conversationClient);

        _logger.StartProcessingActivity();

        CoreActivity activity = await CoreActivity.FromJsonStreamAsync(httpContext.Request.Body, cancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException("Invalid Activity");

        string? correlationVector = httpContext.Request.GetCorrelationVector();
        _logger.ActivityReceived(activity.Type, activity.Id, activity.ServiceUrl, correlationVector);

        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.ReceivedActivityJson(activity.ToJson());
        }

        // TODO: Replace with structured scope data, ensure it works with OpenTelemetry and other logging providers
        using (_logger.BeginActivityScope(activity.Type, activity.Id, activity.ServiceUrl, correlationVector))
        {
            // Use a dedicated timeout instead of the HTTP request's cancellation token.
            // The HTTP token fires when the client disconnects, which is expected for
            // streaming handlers that outlive the original request.
            using CancellationTokenSource cts = new(_processActivityTimeout);
            try
            {
                CancellationToken token = Debugger.IsAttached ? CancellationToken.None : cts.Token;
                await MiddleWare.RunPipelineAsync(this, activity, this.OnActivity, 0, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                _logger.ActivityTimedOut(_processActivityTimeout, activity.Id);
            }
            catch (Exception ex)
            {
                _logger.ActivityProcessingError(ex, activity.Id);
                throw new BotHandlerException("Error processing activity", ex, activity);
            }
            finally
            {
                _logger.ActivityProcessingFinished(activity.Id);
            }
        }
    }

    /// <summary>
    /// Adds the specified turn middleware to the middleware pipeline.
    /// </summary>
    /// <param name="middleware">The middleware component to add to the pipeline. Cannot be null.</param>
    /// <returns>An ITurnMiddleWare instance representing the updated middleware pipeline.</returns>
    public ITurnMiddleware UseMiddleware(ITurnMiddleware middleware)
    {
        ArgumentNullException.ThrowIfNull(middleware);
        MiddleWare.Use(middleware);
        return MiddleWare;
    }

    /// <summary>
    /// Sends the specified activity to the conversation asynchronously.
    /// </summary>
    /// <param name="activity">The activity to send to the conversation. Cannot be null. Must have Conversation.Id set.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the send operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the identifier of the sent activity.</returns>
    /// <exception cref="Exception">Thrown if the conversation client has not been initialized.</exception>
    public async Task<SendActivityResponse?> SendActivityAsync(CoreActivity activity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(_conversationClient, "ConversationClient not initialized");

        return await _conversationClient.SendActivityAsync(activity, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the version of the SDK.
    /// </summary>
    public static string Version => ThisAssembly.NuGetPackageVersion;
}
