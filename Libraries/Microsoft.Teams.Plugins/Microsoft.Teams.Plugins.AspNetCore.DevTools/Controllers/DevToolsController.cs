using System.Net.WebSockets;
using System.Reflection;

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Teams.Plugins.AspNetCore.DevTools.Events;
using Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;

namespace Microsoft.Teams.Plugins.AspNetCore.DevTools.Controllers;

[ApiController]
public class DevToolsController : ControllerBase
{
    private readonly DevToolsPlugin _plugin;
    private readonly IFileProvider _files;
    private readonly IHostApplicationLifetime _lifetime;

    public DevToolsController(DevToolsPlugin plugin, IHostApplicationLifetime lifetime)
    {
        _plugin = plugin;
        _files = new ManifestEmbeddedFileProvider(Assembly.GetExecutingAssembly(), "web");
        _lifetime = lifetime;
    }

    [HttpGet("/devtools")]
    [HttpGet("/devtools/{*path}")]
    public IResult Get(string? path)
    {
        var file = _files.GetFileInfo(path ?? "index.html");

        if (!file.Exists)
        {
            return Get("index.html");
        }

        return Results.File(file.CreateReadStream(), contentType: "text/html");
    }

    [HttpGet("/devtools/sockets")]
    public async Task GetSocket()
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        var id = Guid.NewGuid().ToString();
        var buffer = new byte[1024];

        _plugin.Sockets.Add(id, socket);
        await _plugin.Sockets.Emit(id, new MetaDataEvent(_plugin.MetaData), _lifetime.ApplicationStopping);

        try
        {
            while (socket.State.HasFlag(WebSocketState.Open))
            {
                await socket.ReceiveAsync(buffer, _lifetime.ApplicationStopping);
            }
        }
        catch (ConnectionAbortedException)
        {

        }
        catch (OperationCanceledException)
        {
            
        }
        finally
        {
            if (socket.IsCloseable())
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, _lifetime.ApplicationStopping);
            }
        }

        _plugin.Sockets.Remove(id);
    }
}