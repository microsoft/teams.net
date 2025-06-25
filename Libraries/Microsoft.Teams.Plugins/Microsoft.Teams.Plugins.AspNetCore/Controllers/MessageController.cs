using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps.Extensions;

namespace Microsoft.Teams.Plugins.AspNetCore.Controllers;

[ApiController]
public class MessageController : ControllerBase
{
    private readonly AspNetCorePlugin _plugin;
    private readonly IHostApplicationLifetime _lifetime;

    public MessageController(AspNetCorePlugin plugin, IHostApplicationLifetime lifetime)
    {
        _plugin = plugin;
        _lifetime = lifetime;
    }

    [HttpPost("/api/messages")]
    public async Task<IResult> OnMessage([FromBody] Activity activity)
    {
        var authHeader = HttpContext.Request.Headers.Authorization.FirstOrDefault() ?? throw new UnauthorizedAccessException();
        var token = new JsonWebToken(authHeader.Replace("Bearer ", ""));
        var context = HttpContext.RequestServices.GetRequiredService<TeamsContext>();
        context.Token = token;
        var res = await _plugin.Do(token, activity, null, _lifetime.ApplicationStopping);
        return Results.Json(res.Body, statusCode: (int)res.Status);
    }
}