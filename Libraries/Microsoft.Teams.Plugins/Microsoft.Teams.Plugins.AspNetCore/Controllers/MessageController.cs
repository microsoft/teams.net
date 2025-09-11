// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;

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
    [Authorize(Policy = "TeamsJWTPolicy")]
    public async Task<IResult> OnMessage([FromBody] Activity activity)
    {
        var authHeader = HttpContext.Request.Headers.Authorization.FirstOrDefault() ?? throw new UnauthorizedAccessException();
        var token = new JsonWebToken(authHeader.Replace("Bearer ", ""));
        var data = new Dictionary<string, object?>
        {
            ["Request.TraceId"] = HttpContext.TraceIdentifier
        };

        foreach (var pair in HttpContext.Items)
        {
            var key = pair.Key.ToString();

            if (key is null) continue;

            data[key] = pair.Value;
        }

        var res = await _plugin.Do(new()
        {
            Token = token,
            Activity = activity,
            Extra = data,
            Services = HttpContext.RequestServices
        }, _lifetime.ApplicationStopping);

        // convert response metadata to headers
        foreach (var (key, value) in res.Meta)
        {
            var str = value?.ToString();
            if (string.IsNullOrEmpty(str)) continue;
            Response.Headers.Append($"X-Teams-{char.ToUpper(key[0]) + key[1..]}", str);
        }

        return Results.Json(res.Body, statusCode: (int)res.Status);
    }
}