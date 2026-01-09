// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Core.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.Bot.Core;

/// <summary>
/// Represents a bot application.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "<Pending>")]
public class BotApplication
{
    private readonly ILogger<BotApplication> _logger;
    private readonly ConversationClient? _conversationClient;
    private UserTokenClient? _userTokenClient;
    private readonly string _serviceKey;
    internal TurnMiddleware MiddleWare { get; }

    /// <summary>
    /// Initializes a new instance of the BotApplication class with the specified conversation client, configuration,
    /// logger, and optional service key.
    /// </summary>
    /// <remarks>This constructor sets up the bot application and starts the bot listener using the provided
    /// configuration and service key. The service key is used to locate authentication credentials in the
    /// configuration.</remarks>
    /// <param name="conversationClient">The client used to manage and interact with conversations for the bot.</param>
    /// <param name="userTokenClient">The client used to manage user tokens for authentication.</param>
    /// <param name="config">The application configuration settings used to retrieve environment variables and service credentials.</param>
    /// <param name="logger">The logger used to record operational and diagnostic information for the bot application.</param>
    /// <param name="sectionName">The configuration key identifying the authentication service. Defaults to "AzureAd" if not specified.</param>
    public BotApplication(ConversationClient conversationClient, UserTokenClient userTokenClient, IConfiguration config, ILogger<BotApplication> logger, string sectionName = "AzureAd")
    {
        ArgumentNullException.ThrowIfNull(config);
        _logger = logger;
        _serviceKey = sectionName;
        MiddleWare = new TurnMiddleware();
        _conversationClient = conversationClient;
        _userTokenClient = userTokenClient;
        string appId = config["MicrosoftAppId"] ?? config["CLIENT_ID"] ?? config[$"{sectionName}:ClientId"] ?? "Unknown AppID";
        logger.LogInformation("Started bot listener \n on {Port} \n for AppID:{AppId} \n with SDK version {SdkVersion}", config?["ASPNETCORE_URLS"], appId, Version);

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
    public Func<CoreActivity, CancellationToken, Task<InvokeResponse?>>? OnActivity { get; set; }

    /// <summary>
    /// Processes an incoming HTTP request containing a bot activity.
    /// </summary>
    /// <param name="httpContext"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="BotHandlerException"></exception>
    public async Task<InvokeResponse?> ProcessAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(_conversationClient);

        InvokeResponse? invokeResponse = null;

        _logger.LogDebug("Start processing HTTP request for activity");

        CoreActivity activity = await CoreActivity.FromJsonStreamAsync(httpContext.Request.Body, cancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException("Invalid Activity");

        _logger.LogInformation("Processing activity {Type} {Id}", activity.Type, activity.Id);

        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Received activity: {Activity}", activity.ToJson());
        }

        using (_logger.BeginScope("Processing activity {Type} {Id}", activity.Type, activity.Id))
        {
            try
            {
                invokeResponse =  await MiddleWare.RunPipelineAsync(this, activity, this.OnActivity, 0, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing activity {Type} {Id}", activity.Type, activity.Id);
                throw new BotHandlerException("Error processing activity", ex, activity);
            }
            finally
            {
                _logger.LogInformation("Finished processing activity {Type} {Id}", activity.Type, activity.Id);
            }
            return invokeResponse;
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
