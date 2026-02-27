// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net.Http.Headers;
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
    /// application.
    /// </summary>
    /// <returns>A configured <see cref="TeamsBotApplication"/> instance.</returns>
    public TeamsBotApplication Build()
    {
        _webApp = _webAppBuilder.Build();
        TeamsBotApplication botApp = _webApp.Services.GetService<TeamsBotApplication>() ?? throw new InvalidOperationException("Application not registered");
        _webApp.UseBotApplication<TeamsBotApplication>(_routePath);

        // TODO : review this app builder class
        foreach (var tabAction in _tabActions.ToList())
            tabAction(_webApp);

        foreach (var funcAction in _functionActions.ToList())
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
    public TeamsBotApplicationBuilder WithTab(string name, string physicalPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        ArgumentException.ThrowIfNullOrEmpty(physicalPath, nameof(physicalPath));

#pragma warning disable CA2000 // Dispose objects before losing scope
        return WithTab(name, new PhysicalFileProvider(Path.GetFullPath(physicalPath)));
#pragma warning restore CA2000 // Dispose objects before losing scope
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

        _tabActions.Add(webApp =>
        {
            if (provider is IDisposable disposable)
                webApp.Services.GetRequiredService<IHostApplicationLifetime>()
                    .ApplicationStopped.Register(disposable.Dispose);

            webApp.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = provider,
                RequestPath = $"/tabs/{name}",
                ServeUnknownFileTypes = true
            });

            webApp.MapGet($"/tabs/{name}", () =>
            {
                IFileInfo file = provider.GetFileInfo("/index.html");
                return file.Exists
                    ? Results.File(file.CreateReadStream(), "text/html")
                    : Results.NotFound();
            });

            webApp.MapGet($"/tabs/{name}/{{*path}}", (string path) =>
            {
                IFileInfo file = provider.GetFileInfo($"/{path}");
                if (!file.Exists) return Results.NotFound();
                _contentTypeProvider.TryGetContentType(file.Name, out var contentType);
                return Results.File(file.CreateReadStream(), contentType ?? "application/octet-stream");
            });
        });

        return this;
    }

    /// <summary>
    /// Registers an HTTP POST endpoint at <c>/functions/{name}</c> with a typed request body and typed response.
    /// The endpoint is mapped when <see cref="Build"/> is called.
    /// </summary>
    /// <typeparam name="TBody">The type to deserialize the JSON request body into.</typeparam>
    /// <param name="name">The function name used in the URL path.</param>
    /// <param name="handler">The async handler whose return value is serialized as the JSON response.</param>
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
                FunctionRequest<TBody> request = await httpCtx.Request.ReadFromJsonAsync<FunctionRequest<TBody>>(ct).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("Missing request body.");
                FunctionContext<TBody> ctx = new(botApp)
                {
                    TenantId = httpCtx.User.FindFirst("tid")?.Value,
                    UserId = httpCtx.User.FindFirst("oid")?.Value,
                    UserName = httpCtx.User.FindFirst(ClaimTypes.Name)?.Value,
                    AuthToken = AuthenticationHeaderValue.TryParse(httpCtx.Request.Headers.Authorization.FirstOrDefault(), out var header) ? header.Parameter : null,
                    TeamsContext = request.Context,
                    Data = request.Payload,
                };
                return Results.Json(await handler(ctx, ct).ConfigureAwait(false));
            }).RequireAuthorization(JwtExtensions.EntraPolicy);
        });

        return this;
    }

    /// <summary>
    /// Registers an HTTP POST endpoint at <c>/functions/{name}</c> with no request body.
    /// The endpoint is mapped when <see cref="Build"/> is called.
    /// </summary>
    /// <param name="name">The function name used in the URL path.</param>
    /// <param name="handler">The async handler whose return value is serialized as the JSON response.</param>
    /// <returns>The current instance for fluent chaining.</returns>
    public TeamsBotApplicationBuilder WithFunction(
        string name,
        Func<FunctionContext, CancellationToken, Task<object?>> handler)
        => WithFunction<object>(name, (ctx, ct) => handler(ctx, ct));
}
