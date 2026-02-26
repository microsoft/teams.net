// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Bot.Apps.Routing;
using Microsoft.Teams.Bot.Apps.Schema;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Hosting;

namespace Microsoft.Teams.Bot.Apps;

/// <summary>
/// Teams specific Bot Application
/// </summary>
public class TeamsBotApplication : BotApplication
{
    private readonly TeamsApiClient _teamsApiClient;
    private static TeamsBotApplicationBuilder? _botApplicationBuilder;
    private static readonly FileExtensionContentTypeProvider _contentTypeProvider = new();

    /// <summary>
    /// Gets the router for dispatching Teams activities to registered routes.
    /// </summary>
    internal Router Router { get; }

    /// <summary>
    /// Gets the client used to interact with the Teams API service.
    /// </summary>
    public TeamsApiClient TeamsApiClient => _teamsApiClient;

    private static WebApplication WebApp => _botApplicationBuilder?.WebApplication
        ?? throw new InvalidOperationException("Call Build() first.");


    /// <param name="conversationClient"></param>
    /// <param name="userTokenClient"></param>
    /// <param name="teamsApiClient"></param>
    /// <param name="options">Options containing the application (client) ID, used for logging and diagnostics.</param>
    /// <param name="httpContextAccessor"></param>
    /// <param name="logger"></param>
    public TeamsBotApplication(
        ConversationClient conversationClient,
        UserTokenClient userTokenClient,
        TeamsApiClient teamsApiClient,
        BotApplicationOptions options,
        IHttpContextAccessor httpContextAccessor,
        ILogger<TeamsBotApplication> logger)
        : base(conversationClient, userTokenClient, options, logger)
    {
        _teamsApiClient = teamsApiClient;
        Router = new Router(logger);
        OnActivity = async (activity, cancellationToken) =>
        {
            logger.LogInformation("New {Type} activity received.", activity.Type);
            TeamsActivity teamsActivity = TeamsActivity.FromActivity(activity);
            Context<TeamsActivity> defaultContext = new(this, teamsActivity);

            if (teamsActivity.Type != TeamsActivityType.Invoke)
            {
                await Router.DispatchAsync(defaultContext, cancellationToken).ConfigureAwait(false);
            }
            else // invokes
            {
                InvokeResponse invokeResponse = await Router.DispatchWithReturnAsync(defaultContext, cancellationToken).ConfigureAwait(false);
                HttpContext? httpContext = httpContextAccessor.HttpContext;
                if (httpContext is not null && invokeResponse is not null)
                {
                    httpContext.Response.StatusCode = invokeResponse.Status;
                    logger.LogTrace("Sending invoke response with status {Status} and Body {Body}", invokeResponse.Status, invokeResponse.Body);
                    await httpContext.Response.WriteAsJsonAsync(invokeResponse.Body, cancellationToken).ConfigureAwait(false);

                }
            }
        };
    }

    /// <summary>
    /// Creates a new instance of the TeamsBotApplicationBuilder to configure and build a Teams bot application.
    /// </summary>
    /// <returns></returns>
    public static TeamsBotApplicationBuilder CreateBuilder(string[] args)
    {
        _botApplicationBuilder = new TeamsBotApplicationBuilder(args);
        return _botApplicationBuilder;
    }

    /// <summary>
    /// Registers a tab to be hosted at <c>/tabs/{name}</c>, serving files from the given physical directory.
    /// </summary>
    /// <param name="name">The tab name used in the URL path.</param>
    /// <param name="physicalPath">Absolute or relative path to the directory containing the tab's static files.</param>
    /// <returns>The current instance for fluent chaining.</returns>
    // TODO : breaking change to have withTab instead of AddTab
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "Provider is disposed in catch on failure; on success disposal is registered with IHostApplicationLifetime.ApplicationStopped.")]
    public TeamsBotApplication
        WithTab(string name, string physicalPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        ArgumentException.ThrowIfNullOrEmpty(physicalPath, nameof(physicalPath));
        PhysicalFileProvider provider = new(Path.GetFullPath(physicalPath));
        try
        {
            WebApp.Services.GetRequiredService<IHostApplicationLifetime>()
                .ApplicationStopped.Register(provider.Dispose);
            return WithTab(name, provider);
        }
        catch
        {
            provider.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Registers a tab to be hosted at <c>/tabs/{name}</c>, serving files from the given file provider.
    /// </summary>
    /// <param name="name">The tab name used in the URL path.</param>
    /// <param name="provider">File provider that supplies the tab's static files (e.g. embedded resources).</param>
    /// <returns>The current instance for fluent chaining.</returns>
    // TODO : breaking change to have withTab instead of AddTab
    public TeamsBotApplication WithTab(string name, IFileProvider provider)
    {
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        ArgumentNullException.ThrowIfNull(provider, nameof(provider));

        WebApp.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = provider,
            RequestPath = $"/tabs/{name}",
            ServeUnknownFileTypes = true
        });

        WebApp.MapGet($"/tabs/{name}", () =>
        {
            IFileInfo file = provider.GetFileInfo("index.html");
            return file.Exists
                ? Results.File(file.CreateReadStream(), "text/html")
                : Results.NotFound();
        });

        WebApp.MapGet($"/tabs/{name}/{{*path}}", (string path) =>
        {
            IFileInfo file = provider.GetFileInfo(path);
            if (!file.Exists) return Results.NotFound();
            _contentTypeProvider.TryGetContentType(file.Name, out var contentType);
            return Results.File(file.CreateReadStream(), contentType ?? "application/octet-stream");
        });

        return this;
    }

    /// <summary>
    /// Registers an HTTP POST endpoint at <c>/functions/{name}</c> with a typed request body.
    /// Client context (tenant, user, conversation) is populated from the validated auth token and
    /// <c>X-Teams-*</c> request headers. The deserialized body is available via <see cref="FunctionContext{T}.Data"/>.
    /// </summary>
    /// <typeparam name="TBody">The type to deserialize the JSON request body into.</typeparam>
    /// <param name="name">The function name used in the URL path.</param>
    /// <param name="handler">The async handler. Its return value is serialized as the JSON response.</param>
    /// <returns>The current instance for fluent chaining.</returns>
    // TODO : breaking change to have withFunction instead of AddFunction
    public TeamsBotApplication WithFunction<TBody>(
        string name,
        Func<FunctionContext<TBody>, CancellationToken, Task<object?>> handler)
    {
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        ArgumentNullException.ThrowIfNull(handler, nameof(handler));

        WebApp.MapPost($"/functions/{name}", async (HttpContext httpCtx, CancellationToken ct) =>
        {

            ILogger logger = httpCtx.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger($"functions.{name}");
            TBody? body = await httpCtx.Request.ReadFromJsonAsync<TBody>(ct).ConfigureAwait(false);
            FunctionContext<TBody> ctx = new(this, logger, body!);
            PopulateClientContext(ctx, httpCtx);
            var result = await handler(ctx, ct).ConfigureAwait(false);
            return Results.Json(result);
        }).RequireAuthorization(JwtExtensions.EntraPolicy);

        return this;
    }

    /// <summary>
    /// Registers an HTTP POST endpoint at <c>/functions/{name}</c> with no request body.
    /// Client context (tenant, user, conversation) is populated from the validated auth token and
    /// <c>X-Teams-*</c> request headers.
    /// </summary>
    /// <param name="name">The function name used in the URL path, e.g. <c>"who-am-i"</c>.</param>
    /// <param name="handler">The async handler. Its return value is serialized as the JSON response.</param>
    /// <returns>The current instance for fluent chaining.</returns>
    // TODO : breaking change to have withFunction instead of AddFunction
    public TeamsBotApplication WithFunction(
        string name,
        Func<FunctionContext, CancellationToken, Task<object?>> handler)
    {
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        ArgumentNullException.ThrowIfNull(handler, nameof(handler));

        WebApp.MapPost($"/functions/{name}", async (HttpContext httpCtx, CancellationToken ct) =>
        {
            ILogger logger = httpCtx.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger($"functions.{name}");
            FunctionContext ctx = new(this, logger);
            PopulateClientContext(ctx, httpCtx);
            var result = await handler(ctx, ct).ConfigureAwait(false);
            return Results.Json(result);
        }).RequireAuthorization(JwtExtensions.EntraPolicy);

        return this;
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

    private static void PopulateClientContext(FunctionContext ctx, HttpContext httpCtx)
    {
        BotApplicationOptions botOptions = httpCtx.RequestServices.GetRequiredService<BotApplicationOptions>();
        ctx.BotId = botOptions.AppId;
        ctx.ServiceUrl = botOptions.ServiceUrl;

        ctx.TenantId  = httpCtx.User.FindFirst("tid")?.Value;
        ctx.UserId    = httpCtx.User.FindFirst("oid")?.Value;
        ctx.UserName  = httpCtx.User.FindFirst(ClaimTypes.Name)?.Value;
        ctx.AuthToken = httpCtx.Request.Headers.Authorization.FirstOrDefault()
            ?.Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase);

        // X-Teams-* headers sent by the Teams JS client
        if (httpCtx.Request.Headers.TryGetValue("X-Teams-App-Session-Id", out var appSessionId))
            ctx.AppSessionId = appSessionId;
        if (httpCtx.Request.Headers.TryGetValue("X-Teams-Page-Id", out var pageId))
            ctx.PageId = pageId;
        if (httpCtx.Request.Headers.TryGetValue("X-Teams-Channel-Id", out var channelId))
            ctx.ChannelId = channelId;
        if (httpCtx.Request.Headers.TryGetValue("X-Teams-Chat-Id", out var chatId))
            ctx.ChatId = chatId;
        if (httpCtx.Request.Headers.TryGetValue("X-Teams-Meeting-Id", out var meetingId))
            ctx.MeetingId = meetingId;
        if (httpCtx.Request.Headers.TryGetValue("X-Teams-Team-Id", out var teamId))
            ctx.TeamId = teamId;
        if (httpCtx.Request.Headers.TryGetValue("X-Teams-Message-Id", out var messageId))
            ctx.MessageId = messageId;
        if (httpCtx.Request.Headers.TryGetValue("X-Teams-Sub-Page-Id", out var subPageId))
            ctx.SubPageId = subPageId;
    }
}
