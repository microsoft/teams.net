// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using System.Text;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Events;
using Microsoft.Teams.Apps.Plugins;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Common.Text;
using Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.DevTools.Models;

namespace Microsoft.Teams.Plugins.AspNetCore.DevTools;

[Plugin]
public class DevToolsPlugin : IAspNetCorePlugin
{
    [Dependency("AppId", optional: true)]
    public string? AppId { get; set; }

    [Dependency("AppName", optional: true)]
    public string? AppName { get; set; }

    public event EventFunction Events;

    internal MetaData MetaData => new() { Id = AppId, Name = AppName, Pages = _pages };
    internal readonly WebSocketCollection Sockets = [];

    private readonly ISenderPlugin _sender;
    private readonly IServiceProvider _services;
    private readonly IList<Page> _pages = [];
    private readonly TeamsDevToolsSettings _settings;
    private readonly ILogger<DevToolsPlugin> Logger;

    public DevToolsPlugin(AspNetCorePlugin sender, IServiceProvider provider, ILogger<DevToolsPlugin>? logger = null)
    {
        _sender = sender;
        _services = provider;
        _settings = provider.GetRequiredService<TeamsDevToolsSettings>();
        Logger = logger ?? LoggerFactory.Create(builder => { }).CreateLogger<DevToolsPlugin>();
    }

    public IApplicationBuilder Configure(IApplicationBuilder builder)
    {
        builder.UseWebSockets(new WebSocketOptions()
        {
            AllowedOrigins = { "*" }
        });

        builder.UseStaticFiles(new StaticFileOptions()
        {
            FileProvider = new ManifestEmbeddedFileProvider(Assembly.GetExecutingAssembly(), "web"),
            ServeUnknownFileTypes = true,
            RequestPath = "/devtools"
        });

        builder.Use(async (context, next) =>
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "http error");
                throw new Exception(ex.Message, innerException: ex);
            }
        });

        return builder;
    }

    public DevToolsPlugin AddPage(Page page)
    {
        _pages.Add(page);
        Logger.LogDebug($"page '{page.Name}' added at '{page.Url}'");
        return this;
    }

    public Task OnInit(App app, CancellationToken cancellationToken = default)
    {
        foreach (var page in _settings.Pages)
        {
            AddPage(page);
        }

        Logger.LogWarning(
            new StringBuilder()
                .Bold(
                    new StringBuilder()
                        .Yellow("⚠️  Devtools are not secure and should not be used production environments ⚠️")
                        .ToString()
                ).ToString()
        );

        return Task.CompletedTask;
    }

    public Task OnStart(App app, CancellationToken cancellationToken = default)
    {
        var server = _services.GetRequiredService<IServer>();
        var addresses = server.Features.GetRequiredFeature<IServerAddressesFeature>().Addresses;

        foreach (var address in addresses)
        {
            Logger.LogInformation($"Available at {address}/devtools");
        }

        Logger.LogDebug("OnStart");
        return Task.CompletedTask;
    }

    public Task OnError(App app, IPlugin plugin, ErrorEvent @event, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("OnError");
        return Task.CompletedTask;
    }

    public async Task OnActivity(App app, ISenderPlugin sender, ActivityEvent @event, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("OnActivity");

        await Sockets.Emit(
            DevTools.Events.ActivityEvent.Received(
                @event.Activity,
                @event.Activity.Conversation
            ),
            cancellationToken
        );
    }

    public async Task OnActivitySent(App app, ISenderPlugin sender, ActivitySentEvent @event, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("OnActivitySent");

        await Sockets.Emit(
            DevTools.Events.ActivityEvent.Sent(
                @event.Activity,
                @event.Activity.Conversation
            ),
            cancellationToken
        );
    }

    public Task OnActivityResponse(App app, ISenderPlugin sender, ActivityResponseEvent @event, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("OnActivityResponse");
        return Task.CompletedTask;
    }

    public Task<Response> Do(ActivityEvent @event, CancellationToken cancellationToken = default)
    {
        return _sender.Do(@event, cancellationToken);
    }
}
