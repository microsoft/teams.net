// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Bot.Core.Hosting;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Core;

/// <summary>
/// Represents a bot application.
/// </summary>
public class BotApplication
{
    private readonly ILogger<BotApplication> _logger;
    private readonly ConversationClient? _conversationClient;
    private readonly UserTokenClient? _userTokenClient;
    internal TurnMiddleware MiddleWare { get; }

    /// <summary>
    /// Initializes a new instance of the BotApplication class with the specified conversation client, app ID,
    /// and logger.
    /// Initializes a new instance of the BotApplication class with the specified conversation client, app ID,
    /// and logger.
    /// </summary>
    /// <param name="conversationClient">The client used to manage and interact with conversations for the bot.</param>
    /// <param name="userTokenClient">The client used to manage user tokens for authentication.</param>
    /// <param name="logger">The logger used to record operational and diagnostic information for the bot application.</param>
    /// <param name="options">Options containing the application (client) ID, used for logging and diagnostics. Defaults to an empty instance if not provided.</param>
    public BotApplication(ConversationClient conversationClient, UserTokenClient userTokenClient, ILogger<BotApplication> logger, BotApplicationOptions? options = null)
    {
        options ??= new();
        _logger = logger;
        MiddleWare = new TurnMiddleware();
        _conversationClient = conversationClient;
        _userTokenClient = userTokenClient;
        logger.LogInformation("Started {ThisType} listener for AppID:{AppId} with SDK version {SdkVersion}", this.GetType().Name, options.AppId, Version);
        logger.LogInformation("Started {ThisType} listener for AppID:{AppId} with SDK version {SdkVersion}", this.GetType().Name, options.AppId, Version);
    }


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
    public Func<CoreActivity, CancellationToken, Task>? OnActivity { get; set; }

    /// <summary>
    /// Processes an incoming HTTP request containing a bot activity.
    /// </summary>
    /// <param name="httpContext"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="BotHandlerException"></exception>
    public async Task ProcessAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(_conversationClient);

        _logger.LogDebug("Start processing HTTP request for activity");

        CoreActivity activity = await CoreActivity.FromJsonStreamAsync(httpContext.Request.Body, cancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException("Invalid Activity");

        _logger.LogInformation("Activity received: Type={Type} Id={Id} ServiceUrl={ServiceUrl} MSCV={MSCV}",
            activity.Type,
            activity.Id,
            activity.ServiceUrl,
            httpContext.Request.GetCorrelationVector());

        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Received activity: {Activity}", activity.ToJson());
        }

        // TODO: Replace with structured scope data, ensure it works with OpenTelemetry and other logging providers
        using (_logger.BeginScope("ActivityType={ActivityType} ActivityId={ActivityId} ServiceUrl={ServiceUrl} MSCV={MSCV}",
            activity.Type, activity.Id, activity.ServiceUrl, httpContext.Request.GetCorrelationVector()))
        {
            try
            {
                var token = Debugger.IsAttached ? CancellationToken.None : cancellationToken;
                await MiddleWare.RunPipelineAsync(this, activity, this.OnActivity, 0, token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing activity: Id={Id}", activity.Id);
                throw new BotHandlerException("Error processing activity", ex, activity);
            }
            finally
            {
                _logger.LogInformation("Finished processing activity: Id={Id}", activity.Id);
            }
        }
    }

    /// <summary>
    /// Adds the specified turn middleware to the middleware pipeline.
    /// </summary>
    /// <param name="middleware">The middleware component to add to the pipeline. Cannot be null.</param>
    /// <returns>An ITurnMiddleWare instance representing the updated middleware pipeline.</returns>
    public ITurnMiddleWare Use(ITurnMiddleWare middleware)
    {
        MiddleWare.Use(middleware);
        return MiddleWare;
    }

    /// <summary>
    /// Sends the specified activity to the conversation asynchronously.
    /// </summary>
    /// <param name="activity">The activity to send to the conversation. Cannot be null.</param>
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
