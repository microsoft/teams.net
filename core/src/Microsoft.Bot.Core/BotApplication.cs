

using System.Diagnostics;

using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Core.Hosting;
using Microsoft.Bot.Core.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.Bot.Core;

/// <summary>
/// Represents a bot application.
/// </summary>
public class BotApplication
{
    private readonly ILogger<BotApplication> _logger;
    private readonly IConfiguration _configuration;
    private readonly ConversationClient? _conversationClient;
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
    /// <param name="config">The application configuration settings used to retrieve environment variables and service credentials.</param>
    /// <param name="logger">The logger used to record operational and diagnostic information for the bot application.</param>
    /// <param name="serviceKey">The configuration key identifying the authentication service. Defaults to "AzureAd" if not specified.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "<Pending>")]
    public BotApplication(ConversationClient conversationClient, IConfiguration config, ILogger<BotApplication> logger, string serviceKey = "AzureAd")
    {
        _logger = logger;
        _configuration = config;
        _serviceKey = serviceKey;
        MiddleWare = new TurnMiddleware();
        _conversationClient = conversationClient;
        logger.LogInformation("Started bot listener on {Port} for AppID:{AppId}", config?["ASPNETCORE_URLS"], config?[$"{_serviceKey}:ClientId"]);
    }


    /// <summary>
    /// Gets the client used to manage and interact with conversations.
    /// </summary>
    /// <remarks>Accessing this property before the client is initialized will result in an exception. Ensure
    /// that the client is properly configured before use.</remarks>
    public ConversationClient ConversationClient => _conversationClient ?? throw new InvalidOperationException("ConversationClient not initialized");

    /// <summary>
    /// Gets or sets the delegate that is invoked to handle an incoming activity asynchronously.
    /// </summary>
    /// <remarks>Assign a delegate to process activities as they are received. The delegate should accept an
    /// <see cref="Activity"/> and a <see cref="CancellationToken"/>, and return a <see cref="Task"/> representing the
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
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "<Pending>")]
    public async Task<CoreActivity> ProcessAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        if (_conversationClient is null)
        {
            throw new InvalidOperationException("BotApplication not initialized with ConversationClient");
        }

        CoreActivity activity = await CoreActivity.FromJsonStreamAsync(httpContext.Request.Body, cancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException("Invalid Activity");

        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Received activity: {Activity}", activity.ToJson());
        }

        AgenticIdentity? agenticIdentity = AgenticIdentity.FromProperties(activity.Recipient!.Properties!);
        _conversationClient.AgenticIdentity = agenticIdentity;

        using (_logger.BeginScope("Processing activity {Type} {Id}", activity.Type, activity.Id))
        {
            try
            {
                await MiddleWare.RunPipelineAsync(this, activity, this.OnActivity, 0, cancellationToken).ConfigureAwait(false);
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
            return activity;
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
    public async Task<string> SendActivityAsync(CoreActivity activity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(_conversationClient, "ConversationClient not initialized");

        return await _conversationClient.SendActivityAsync(activity, cancellationToken).ConfigureAwait(false);
    }
}
