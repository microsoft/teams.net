using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Plugins.AspNetCore;

namespace Samples.Repro.DA;

[ApiController]
[Route("api/messages")]
public class BotController : ControllerBase
{
    private readonly App _app;

    public BotController(App app)
    {
        _app = app;
    }

    [HttpPost]
    public async Task<IResult> ProcessMessage()
    {
        var sw = Stopwatch.StartNew();
        HttpContext.Items[TimingConstants.EndpointStopwatchKey] = sw;

        _app.Logger.Info("endpoint: request received");

        var plugin = _app.GetPlugin<AspNetCorePlugin>();
        if (plugin is null)
        {
            return Results.Problem("AspNetCorePlugin not registered.", statusCode: 500);
        }

        var result = await plugin.Do(HttpContext, HttpContext.RequestAborted);

        _app.Logger.Info($"endpoint: done in {sw.ElapsedMilliseconds}ms");
        return result;
    }
}
