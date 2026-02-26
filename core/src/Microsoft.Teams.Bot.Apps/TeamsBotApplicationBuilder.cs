// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Bot.Core.Hosting;

namespace Microsoft.Teams.Bot.Apps;

/// <summary>
/// Teams Bot Application Builder to configure and build a Teams bot application.
/// </summary>
public class TeamsBotApplicationBuilder
{
    private readonly WebApplicationBuilder _webAppBuilder;
    private WebApplication? _webApp;
    private string _routePath = "/api/messages";
    private readonly List<Action<WebApplication>> _tabActions = [];
    private readonly List<Action<WebApplication, TeamsBotApplication>> _functionActions = [];
    private static readonly FileExtensionContentTypeProvider _contentTypeProvider = new();

    internal WebApplication WebApplication => _webApp ?? throw new InvalidOperationException("Call Build");

    /// <summary>
    /// Accessor for the service collection used to configure application services.
    /// </summary>
    public IServiceCollection Services => _webAppBuilder.Services;

    /// <summary>
    /// Accessor for the application configuration used to configure services and settings.
    /// </summary>
    public IConfiguration Configuration => _webAppBuilder.Configuration;

    /// <summary>
    /// Accessor for the web hosting environment information.
    /// </summary>
    public IWebHostEnvironment Environment => _webAppBuilder.Environment;

    /// <summary>
    /// Accessor for configuring the host settings and services.
    /// </summary>
    public ConfigureHostBuilder Host => _webAppBuilder.Host;

    /// <summary>
    /// Accessor for configuring logging services and settings.
    /// </summary>
    public ILoggingBuilder Logging => _webAppBuilder.Logging;

    /// <summary>
    /// Creates a new instance of the BotApplicationBuilder with default configuration and registered bot services.
    /// </summary>
    public TeamsBotApplicationBuilder(string[] args)
    {
        _webAppBuilder = WebApplication.CreateSlimBuilder(args);
        _webAppBuilder.Services.AddHttpContextAccessor();
        _webAppBuilder.Services.AddTeamsBotApplication();
    }

    /// <summary>
    /// Builds and configures the bot application pipeline, returning a fully initialized instance of the bot
    /// application. All registered tabs and functions are mapped to the web application at this point.
    /// </summary>
    /// <returns>A configured <see cref="TeamsBotApplication"/> instance.</returns>
    public TeamsBotApplication Build()
    {
        _webApp = _webAppBuilder.Build();
        TeamsBotApplication botApp = _webApp.Services.GetService<TeamsBotApplication>() ?? throw new InvalidOperationException("Application not registered");
        _webApp.UseBotApplication<TeamsBotApplication>(_routePath);

        foreach (var tabAction in _tabActions)
            tabAction(_webApp);

        foreach (var funcAction in _functionActions)
            funcAction(_webApp, botApp);

        return botApp;
    }

    /// <summary>
    /// Sets the route path used to handle incoming bot requests. Defaults to "/api/messages".
    /// </summary>
    /// <param name="routePath">The route path to use for bot endpoints. Cannot be null or empty.</param>
    /// <returns>The current instance of <see cref="TeamsBotApplicationBuilder"/> for method chaining.</returns>
    public TeamsBotApplicationBuilder WithRoutePath(string routePath)
    {
        _routePath = routePath;
        return this;
    }

    /// <summary>
    /// Registers a tab to be hosted at <c>/tabs/{name}</c>, serving files from the given physical directory.
    /// Routes are mapped when <see cref="Build"/> is called.
    /// </summary>
    /// <param name="name">The tab name used in the URL path.</param>
    /// <param name="physicalPath">Absolute or relative path to the directory containing the tab's static files.</param>
    /// <returns>The current instance for fluent chaining.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "Provider is disposed in catch on failure; on success disposal is registered with IHostApplicationLifetime.ApplicationStopped.")]
    public TeamsBotApplicationBuilder WithTab(string name, string physicalPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        ArgumentException.ThrowIfNullOrEmpty(physicalPath, nameof(physicalPath));

        _tabActions.Add(webApp =>
        {
            PhysicalFileProvider provider = new(Path.GetFullPath(physicalPath));
            try
            {
                webApp.Services.GetRequiredService<IHostApplicationLifetime>()
                    .ApplicationStopped.Register(provider.Dispose);
                ApplyTab(webApp, name, provider);
            }
            catch
            {
                provider.Dispose();
                throw;
            }
        });

        return this;
    }

    /// <summary>
    /// Registers a tab to be hosted at <c>/tabs/{name}</c>, serving files from the given file provider.
    /// Routes are mapped when <see cref="Build"/> is called.
    /// </summary>
    /// <param name="name">The tab name used in the URL path.</param>
    /// <param name="provider">File provider that supplies the tab's static files.</param>
    /// <returns>The current instance for fluent chaining.</returns>
    public TeamsBotApplicationBuilder WithTab(string name, IFileProvider provider)
    {
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        ArgumentNullException.ThrowIfNull(provider, nameof(provider));

        _tabActions.Add(webApp => ApplyTab(webApp, name, provider));

        return this;
    }

    /// <summary>
    /// Registers an HTTP POST endpoint at <c>/functions/{name}</c> with a typed request body.
    /// The endpoint is mapped when <see cref="Build"/> is called.
    /// </summary>
    /// <typeparam name="TBody">The type to deserialize the JSON request body into.</typeparam>
    /// <param name="name">The function name used in the URL path.</param>
    /// <param name="handler">The async handler. Its return value is serialized as the JSON response.</param>
    /// <returns>The current instance for fluent chaining.</returns>
    public TeamsBotApplicationBuilder WithFunction<TBody>(
        string name,
        Func<FunctionContext<TBody>, CancellationToken, Task<object?>> handler)
    {
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        ArgumentNullException.ThrowIfNull(handler, nameof(handler));

        _functionActions.Add((webApp, botApp) =>
        {
            webApp.MapPost($"/functions/{name}", async (HttpContext httpCtx, CancellationToken ct) =>
            {
                ILogger logger = httpCtx.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger($"functions.{name}");
                TBody? body = await httpCtx.Request.ReadFromJsonAsync<TBody>(ct).ConfigureAwait(false);
                FunctionContext<TBody> ctx = new(botApp, logger, body!);
                PopulateClientContext(ctx, httpCtx);
                var result = await handler(ctx, ct).ConfigureAwait(false);
                return Results.Json(result);
            }).RequireAuthorization(JwtExtensions.EntraPolicy);
        });

        return this;
    }

    /// <summary>
    /// Registers an HTTP POST endpoint at <c>/functions/{name}</c> with no request body.
    /// The endpoint is mapped when <see cref="Build"/> is called.
    /// </summary>
    /// <param name="name">The function name used in the URL path.</param>
    /// <param name="handler">The async handler. Its return value is serialized as the JSON response.</param>
    /// <returns>The current instance for fluent chaining.</returns>
    public TeamsBotApplicationBuilder WithFunction(
        string name,
        Func<FunctionContext, CancellationToken, Task<object?>> handler)
    {
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        ArgumentNullException.ThrowIfNull(handler, nameof(handler));

        _functionActions.Add((webApp, botApp) =>
        {
            webApp.MapPost($"/functions/{name}", async (HttpContext httpCtx, CancellationToken ct) =>
            {
                ILogger logger = httpCtx.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger($"functions.{name}");
                FunctionContext ctx = new(botApp, logger);
                PopulateClientContext(ctx, httpCtx);
                var result = await handler(ctx, ct).ConfigureAwait(false);
                return Results.Json(result);
            }).RequireAuthorization(JwtExtensions.EntraPolicy);
        });

        return this;
    }

    private static void ApplyTab(WebApplication webApp, string name, IFileProvider provider)
    {
        webApp.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = provider,
            RequestPath = $"/tabs/{name}",
            ServeUnknownFileTypes = true
        });

        webApp.MapGet($"/tabs/{name}", () =>
        {
            IFileInfo file = provider.GetFileInfo("index.html");
            return file.Exists
                ? Results.File(file.CreateReadStream(), "text/html")
                : Results.NotFound();
        });

        webApp.MapGet($"/tabs/{name}/{{*path}}", (string path) =>
        {
            IFileInfo file = provider.GetFileInfo(path);
            if (!file.Exists) return Results.NotFound();
            _contentTypeProvider.TryGetContentType(file.Name, out var contentType);
            return Results.File(file.CreateReadStream(), contentType ?? "application/octet-stream");
        });
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
