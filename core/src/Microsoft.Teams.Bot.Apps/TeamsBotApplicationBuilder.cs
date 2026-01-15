// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.Teams.Bot.Apps;

/// <summary>
/// Teams Bot Application Builder to configure and build a Teams bot application.
/// </summary>
public class TeamsBotApplicationBuilder
{
    private readonly WebApplicationBuilder _webAppBuilder;
    private WebApplication? _webApp;
    private string _routePath = "/api/messages";
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
    public TeamsBotApplicationBuilder()
    {
        _webAppBuilder = WebApplication.CreateSlimBuilder();
        _webAppBuilder.Services.AddHttpContextAccessor();
        _webAppBuilder.Services.AddTeamsBotApplication();
    }

    /// <summary>
    /// Builds and configures the bot application pipeline, returning a fully initialized instance of the bot
    /// application.
    /// </summary>
    /// <returns>A configured <see cref="BotApplication"/> instance representing the bot application pipeline.</returns>
    public TeamsBotApplication Build()
    {
        _webApp = _webAppBuilder.Build();
        TeamsBotApplication botApp = _webApp.Services.GetService<TeamsBotApplication>() ?? throw new InvalidOperationException("Application not registered");
        _webApp.UseBotApplication<TeamsBotApplication>(_routePath);
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
}
