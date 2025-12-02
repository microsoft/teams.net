using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Core.Hosting;
using Microsoft.Bot.Core.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using System.Collections;

namespace Microsoft.Bot.Core;

public class BotHanlderException(string message, Exception ex, Activity activity) : Exception(message, ex)
{
    public Activity Activity { get; } = activity;
}

public delegate Task NextDelegate(CancellationToken cancellationToken);
public interface ITurnMiddleWare
{
    Task OnTurnAsync(BotApplication botApplication, Activity activity, NextDelegate next, CancellationToken cancellationToken = default);
}

public class BotApplication
{
    private readonly ILogger<BotApplication> _logger;
    private readonly IConfiguration _configuration;
    private ConversationClient? _conversationClient;
    private UserTokenClient? _userTokenClient;
    private readonly string _serviceKey;
    private readonly TurnMiddleware _turnMiddleware;

    public BotApplication()
    {
        _logger = NullLogger<BotApplication>.Instance;
        _configuration = new ConfigurationBuilder().Build();
        _serviceKey = "AzureAd";
        _turnMiddleware = new TurnMiddleware();
    }

    public BotApplication(IConfiguration config, ILogger<BotApplication> logger, string serviceKey = "AzureAd")
    {
        _logger = logger;
        _configuration = config;
        _serviceKey = serviceKey;
        _turnMiddleware = new TurnMiddleware();
        logger.LogInformation("Started bot listener on {port} for AppID:{appid}", config["ASPNETCORE_URLS"], config[$"{_serviceKey}:ClientId"]);
    }

    internal TurnMiddleware MiddleWare => _turnMiddleware;

    public UserTokenClient UserTokenClient => _userTokenClient ?? throw new Exception("UserTokenClient not initialized");

    public Func<Activity, CancellationToken, Task>? OnActivity { get; set; }

    public async Task<Activity> ProcessAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        _conversationClient = httpContext.RequestServices.GetKeyedService<ConversationClient>(_serviceKey) ?? throw new Exception("ConversationClient not registered");

        _userTokenClient = httpContext.RequestServices.GetService<UserTokenClient>() ?? throw new Exception("UserTokenClient not registered");

        Activity activity = await Activity.FromJsonStreamAsync(httpContext.Request.Body, cancellationToken) ?? throw new InvalidOperationException("Invalid Activity");

        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Received activity: {Activity}", activity.ToJson());
        }

        AgenticIdentity? agenticIdentity = AgenticIdentity.FromProperties(activity.Recipient!.Properties!);

        _userTokenClient.AgenticIdentity = agenticIdentity;


        using (_logger.BeginScope("Processing activity {Type} {Id}", activity.Type, activity.Id))
        {
            try
            {
                await _turnMiddleware.RunPipeline(this, activity, this.OnActivity, 0, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing activity {Type} {Id}", activity.Type, activity.Id);
                throw new BotHanlderException("Error processing activity", ex, activity);
            }
            finally
            {
                _logger.LogInformation("Finished processing activity {Type} {Id}", activity.Type, activity.Id);
            }
            return activity;
        }
    }

    public ITurnMiddleWare Use(ITurnMiddleWare middleware)
    {
        _turnMiddleware.Use(middleware);
        return _turnMiddleware;
    }

    public async Task<string> SendActivityAsync(Activity activity, CancellationToken cancellationToken = default)
    {
        if (_conversationClient is null)
        {
            throw new Exception("ConversationClient not initialized");
        }
        return await _conversationClient.SendActivityAsync(activity, cancellationToken);
    }
}

internal class TurnMiddleware : ITurnMiddleWare, IEnumerable<ITurnMiddleWare>
{

    private readonly IList<ITurnMiddleWare> _middlewares = [];
    internal TurnMiddleware Use(ITurnMiddleWare middleware)
    {
        _middlewares.Add(middleware);
        return this;
    }


    public async Task OnTurnAsync(BotApplication botApplication, Activity activity, NextDelegate next, CancellationToken cancellationToken = default)
    {
        await RunPipeline(botApplication, activity, null!, 0, cancellationToken).ConfigureAwait(false);
        await next(cancellationToken).ConfigureAwait(false);
    }

    public Task RunPipeline(BotApplication botApplication, Activity activity, Func<Activity, CancellationToken, Task>? callback, int nextMiddlewareIndex, CancellationToken cancellationToken)
    {
        if (nextMiddlewareIndex == _middlewares.Count)
        {
            if (callback is not null)
            {
                return callback!(activity, cancellationToken) ?? Task.CompletedTask;
            }
            else
            {
                return Task.CompletedTask;
            }
        }
        ITurnMiddleWare nextMiddleware = _middlewares[nextMiddlewareIndex];
        return nextMiddleware.OnTurnAsync(
            botApplication,
            activity,
            (ct) => RunPipeline(botApplication, activity, callback, nextMiddlewareIndex + 1, ct),
            cancellationToken);

    }

    public IEnumerator<ITurnMiddleWare> GetEnumerator()
    {
        return _middlewares.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}