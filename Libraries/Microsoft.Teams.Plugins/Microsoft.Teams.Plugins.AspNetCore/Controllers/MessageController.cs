// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

using static Microsoft.Teams.Plugins.AspNetCore.Extensions.HostApplicationBuilderExtensions;

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
    [Authorize(Policy = TeamsTokenAuthConstants.AuthorizationPolicy)]
    public async Task<IResult> OnMessage()
    {
        return await _plugin.Do(HttpContext, _lifetime.ApplicationStopping);
    }
}