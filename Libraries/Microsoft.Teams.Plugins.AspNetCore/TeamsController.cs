using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps.Extensions;

namespace Microsoft.Teams.Plugins.AspNetCore;

[ApiController]
public class TeamsController : ControllerBase
{
    private readonly AspNetCorePlugin _plugin;

    public TeamsController(AspNetCorePlugin plugin)
    {
        _plugin = plugin;
    }

    [HttpPost("/api/messages")]
    public async Task<IResult> OnMessage([FromBody] Activity activity, CancellationToken cancellationToken)
    {
        var authHeader = HttpContext.Request.Headers.Authorization.FirstOrDefault() ?? throw new UnauthorizedAccessException();
        var token = new JsonWebToken(authHeader.Replace("Bearer ", ""));
        var context = HttpContext.RequestServices.GetRequiredService<TeamsContext>();
        context.Token = token;
        var res = await _plugin.Do(token, activity, cancellationToken);
        return Results.Json(res.Body, statusCode: (int)res.Status);
    }
}