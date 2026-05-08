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
/// Represents a bot application that receives and processes activities from a messaging channel.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="BotApplication"/> is the central entry point for handling incoming bot activities.
/// Register it with the host using <see cref="AddBotApplicationExtensions.AddBotApplication"/> and
/// map it to an endpoint with <see cref="AddBotApplicationExtensions.UseBotApplication"/>.
/// </para>
/// <example>
/// <strong>Minimal setup in Program.cs:</strong>
/// <code>
/// var builder = WebApplication.CreateBuilder(args);
/// builder.Services.AddBotApplication();
///
/// var app = builder.Build();
/// var bot = app.UseBotApplication();
///
/// bot.OnActivity = async (activity, ct) =>
/// {
///     await bot.SendActivityAsync(
///         CoreActivity.CreateBuilder()
///             .WithType(ActivityType.Message)
///             .WithConversation(activity.Conversation)
///             .WithServiceUrl(activity.ServiceUrl)
///             .WithProperty("text", "Hello!")
///             .Build(),
///         ct);
/// };
///
/// app.Run();
/// </code>
/// </example>
/// <example>
/// <strong>Subclassing for more complex scenarios:</strong>
/// <code>
/// public class MyBot : BotApplication
/// {
///     public MyBot(ConversationClient conversationClient, UserTokenClient userTokenClient, ILogger&lt;MyBot&gt; logger)
///         : base(conversationClient, userTokenClient, logger)
///     {
///         OnActivity = HandleActivityAsync;
///     }
///
///     private async Task HandleActivityAsync(CoreActivity activity, CancellationToken ct)
///     {
///         if (activity.Type == ActivityType.Message)
///         {
///             // Echo the user's message back
///             await SendActivityAsync(
///                 CoreActivity.CreateBuilder()
///                     .WithType(ActivityType.Message)
///                     .WithConversation(activity.Conversation)
///                     .WithServiceUrl(activity.ServiceUrl)
///                     .WithProperty("text", $"You said: {activity.Properties["text"]}")
///                     .Build(),
///                 ct);
///         }
///     }
/// }
/// </code>
/// </example>
/// </remarks>
public class BotApplication
{
    private readonly ILogger<BotApplication> _logger;
    private readonly ConversationClient? _conversationClient;
    private readonly UserTokenClient? _userTokenClient;
    private readonly TimeSpan _processActivityTimeout = TimeSpan.FromMinutes(5);
    internal TurnMiddleware MiddleWare { get; }

    /// <summary>
    /// Creates a default instance, primarily for testing purposes.
    /// The <see cref="ConversationClient"/> and <see cref="UserTokenClient"/> properties will not be initialized;
    /// accessing them will throw <see cref="InvalidOperationException"/>.
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
    /// Gets the application (client) ID configured for this bot (for example, the Azure AD app registration client ID).
    /// </summary>
    public string AppId { get; }

    /// <summary>
    /// Gets the <see cref="Core.ConversationClient"/> used to send, update, and delete activities in conversations.
    /// </summary>
    /// <remarks>This property is only available when the bot is constructed via dependency injection or
    /// with an explicit <see cref="Core.ConversationClient"/>. It throws <see cref="InvalidOperationException"/>
    /// if accessed on a test instance created with the parameterless constructor.</remarks>
    public ConversationClient ConversationClient => _conversationClient ?? throw new InvalidOperationException("ConversationClient not initialized");

    /// <summary>
    /// Gets the <see cref="Core.UserTokenClient"/> used to manage OAuth user tokens (sign-in, sign-out, token exchange).
    /// </summary>
    /// <remarks>This property is only available when the bot is constructed via dependency injection or
    /// with an explicit <see cref="Core.UserTokenClient"/>. It throws <see cref="InvalidOperationException"/>
    /// if accessed on a test instance created with the parameterless constructor.</remarks>
    public UserTokenClient UserTokenClient => _userTokenClient ?? throw new InvalidOperationException("UserTokenClient not registered");

    /// <summary>
    /// Gets or sets the delegate that is invoked to handle each incoming activity.
    /// </summary>
    /// <remarks>
    /// Assign a handler to process activities as they arrive. If <see langword="null"/>, incoming activities
    /// pass through the middleware pipeline but are otherwise ignored.
    /// <example>
    /// <code>
    /// bot.OnActivity = async (activity, ct) =>
    /// {
    ///     if (activity.Type == ActivityType.Message)
    ///     {
    ///         await bot.SendActivityAsync(
    ///             CoreActivity.CreateBuilder()
    ///                 .WithType(ActivityType.Message)
    ///                 .WithConversation(activity.Conversation)
    ///                 .WithServiceUrl(activity.ServiceUrl)
    ///                 .WithProperty("text", "Received your message!")
    ///                 .Build(),
    ///             ct);
    ///     }
    /// };
    /// </code>
    /// </example>
    /// </remarks>
    public virtual Func<CoreActivity, CancellationToken, Task>? OnActivity { get; set; }

    /// <summary>
    /// Processes an incoming HTTP request containing a bot activity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The request body is deserialized into a <see cref="CoreActivity"/>, run through the registered
    /// middleware pipeline (see <see cref="UseMiddleware"/>), and finally dispatched to <see cref="OnActivity"/>.
    /// </para>
    /// <para>
    /// A dedicated internal timeout (configurable via <see cref="BotApplicationOptions.ProcessActivityTimeout"/>,
    /// default 5 minutes) is used instead of the HTTP request's cancellation token, because streaming handlers
    /// may outlive the original HTTP connection. When a debugger is attached the timeout is disabled.
    /// </para>
    /// </remarks>
    /// <param name="httpContext">The HTTP context containing the incoming bot activity request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the initial deserialization. Note: a dedicated timeout governs activity processing.</param>
    /// <returns>A task that represents the asynchronous activity processing operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the request body cannot be deserialized into a valid activity.</exception>
    /// <exception cref="BotHandlerException">Thrown if an error occurs while processing the activity, wrapping the original exception and the offending <see cref="CoreActivity"/>.</exception>
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
    /// <remarks>
    /// Middleware components execute in the order they are registered. Each middleware can inspect or modify
    /// the activity, perform side effects (such as logging), or short-circuit the pipeline by not calling
    /// <see cref="NextTurn"/>.
    /// <example>
    /// <code>
    /// bot.UseMiddleware(new MyLoggingMiddleware());
    /// bot.UseMiddleware(new MyAuthMiddleware());
    /// // Pipeline order: MyLoggingMiddleware → MyAuthMiddleware → OnActivity
    /// </code>
    /// </example>
    /// </remarks>
    /// <param name="middleware">The middleware component to add to the pipeline. Cannot be null.</param>
    /// <returns>The <see cref="ITurnMiddleware"/> instance representing the middleware pipeline.</returns>
    public ITurnMiddleware UseMiddleware(ITurnMiddleware middleware)
    {
        ArgumentNullException.ThrowIfNull(middleware);
        MiddleWare.Use(middleware);
        return MiddleWare;
    }

    /// <summary>
    /// Sends the specified activity to the conversation asynchronously.
    /// </summary>
    /// <remarks>
    /// This is a convenience wrapper around <see cref="ConversationClient.SendActivityAsync"/>. The activity
    /// must have its <see cref="CoreActivity.Conversation"/> and <see cref="CoreActivity.ServiceUrl"/> properties set.
    /// <example>
    /// <code>
    /// var reply = CoreActivity.CreateBuilder()
    ///     .WithType(ActivityType.Message)
    ///     .WithConversation(incomingActivity.Conversation)
    ///     .WithServiceUrl(incomingActivity.ServiceUrl)
    ///     .WithProperty("text", "Hello from the bot!")
    ///     .Build();
    ///
    /// SendActivityResponse? response = await bot.SendActivityAsync(reply, cancellationToken);
    /// string? sentId = response?.Id;
    /// </code>
    /// </example>
    /// </remarks>
    /// <param name="activity">The activity to send. Cannot be null. Must have <see cref="CoreActivity.Conversation"/> and <see cref="CoreActivity.ServiceUrl"/> set.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the send operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="SendActivityResponse"/> with the ID of the sent activity, or null.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="activity"/> is null or the conversation client has not been initialized.</exception>
    public async Task<SendActivityResponse?> SendActivityAsync(CoreActivity activity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(_conversationClient, "ConversationClient not initialized");

        return await _conversationClient.SendActivityAsync(activity, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the version of the Microsoft.Teams.Core SDK (for example, <c>"1.0.0"</c>).
    /// </summary>
    public static string Version => ThisAssembly.NuGetPackageVersion;
}
