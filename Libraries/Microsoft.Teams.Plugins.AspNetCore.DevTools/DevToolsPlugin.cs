using System.Diagnostics.CodeAnalysis;
using System.Text;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.Api;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Plugins;
using Microsoft.Teams.Common.Logging;
using Microsoft.Teams.Common.Text;
using Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.DevTools.Models;

namespace Microsoft.Teams.Plugins.AspNetCore.DevTools;

[Plugin]
public class DevToolsPlugin : IAspNetCorePlugin
{
    [AllowNull]
    [Dependency]
    public ILogger Logger { get; set; }

    [Dependency("AppId", optional: true)]
    public string? AppId { get; set; }

    [Dependency("AppName", optional: true)]
    public string? AppName { get; set; }

    public event IPlugin.ErrorEventHandler ErrorEvent = (_, _) => Task.Run(() => { });
    public event IPlugin.ActivityEventHandler ActivityEvent = (_, _, _, _) => Task.FromResult(new Response(System.Net.HttpStatusCode.OK));

    internal MetaData MetaData => new() { Id = AppId, Name = AppName, Pages = _pages };
    internal readonly WebSocketCollection Sockets = [];

    private readonly ISenderPlugin _sender;
    private readonly IServiceProvider _services;
    private readonly IList<Page> _pages = [];
    private readonly TeamsDevToolsSettings _settings;

    public DevToolsPlugin(AspNetCorePlugin sender, IServiceProvider provider)
    {
        _sender = sender;
        _services = provider;
        _settings = provider.GetRequiredService<TeamsDevToolsSettings>();
    }

    public IApplicationBuilder Configure(IApplicationBuilder builder)
    {
        builder.UseWebSockets(new WebSocketOptions()
        {
            AllowedOrigins = { "*" }
        });

        builder.Use(async (context, next) =>
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "http error");
                throw new Exception(ex.Message, innerException: ex);
            }
        });

        return builder;
    }

    public DevToolsPlugin AddPage(Page page)
    {
        _pages.Add(page);
        Logger.Debug($"page '{page.Name}' added at '{page.Url}'");
        return this;
    }

    public Task OnInit(IApp app, CancellationToken cancellationToken = default)
    {
        foreach (var page in _settings.Pages)
        {
            AddPage(page);
        }

        return Task.Run(() =>
        {
            Logger.Warn(
                new StringBuilder()
                    .Bold(
                        new StringBuilder()
                            .Yellow("⚠️  Devtools are not secure and should not be used production environments ⚠️")
                            .ToString()
                    )
            );
        });
    }

    public Task OnStart(IApp app, CancellationToken cancellationToken = default)
    {
        var server = _services.GetRequiredService<IServer>();
        var addresses = server.Features.GetRequiredFeature<IServerAddressesFeature>().Addresses;

        foreach (var address in addresses)
        {
            Logger.Info($"Available at {address}/devtools");
        }

        return Task.Run(() => Logger.Debug("OnStart"));
    }

    public Task OnError(IApp app, IPlugin? plugin, Exception exception, IContext<IActivity>? context, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Logger.Debug("OnError"));
    }

    public async Task OnActivity(IApp app, IContext<IActivity> context)
    {
        Logger.Debug("OnActivity");
        await Sockets.Emit(Events.ActivityEvent.Received(
            context.Activity,
            context.Ref.Conversation
        ), context.CancellationToken);
    }

    public async Task OnActivitySent(IApp app, IActivity activity, IContext<IActivity> context)
    {
        Logger.Debug("OnActivitySent");
        await Sockets.Emit(
            Events.ActivityEvent.Sent(activity, context.Ref.Conversation),
            context.CancellationToken
        );
    }

    public async Task OnActivitySent(IApp app, ISenderPlugin sender, IActivity activity, ConversationReference reference, CancellationToken cancellationToken = default)
    {
        Logger.Debug("OnActivitySent");
        await Sockets.Emit(
            Events.ActivityEvent.Sent(activity, reference.Conversation),
            cancellationToken
        );
    }

    public Task OnActivityResponse(IApp app, Response? response, IContext<IActivity> context)
    {
        return Task.Run(() => Logger.Debug("OnActivityResponse"));
    }

    public Task<Response> Do(IToken token, IActivity activity, CancellationToken cancellationToken = default)
    {
        return _sender.Do(token, activity, cancellationToken);
    }
}