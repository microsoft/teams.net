﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
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
    public async Task<IResult> OnMessage([FromBody] Activity activity)
    {
        var scope = HttpContext.RequestServices.CreateScope();
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
            Services = scope.ServiceProvider
        }, _lifetime.ApplicationStopping);

        return Results.Json(res.Body, statusCode: (int)res.Status);
    }
}