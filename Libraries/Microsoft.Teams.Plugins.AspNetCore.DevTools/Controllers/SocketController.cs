using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Teams.Plugins.AspNetCore.DevTools.Events;

namespace Microsoft.Teams.Plugins.AspNetCore.DevTools.Controllers;

[ApiController]
public class SocketController : ControllerBase
{
    private readonly DevToolsPlugin _plugin;

    public SocketController(DevToolsPlugin plugin)
    {
        _plugin = plugin;
    }

    [HttpGet("/devtools/sockets")]
    public async Task OnSocket(CancellationToken cancellationToken)
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
        await _plugin.Sockets.Emit(id, new MetaDataEvent(_plugin.MetaData), cancellationToken);

        while (
            socket.State == System.Net.WebSockets.WebSocketState.Open &&
            !cancellationToken.IsCancellationRequested
        )
        {
            await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
            Thread.Sleep(200);
        }

        _plugin.Sockets.Remove(id);
    }
}