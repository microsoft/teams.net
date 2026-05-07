// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Teams.Api;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;

namespace Microsoft.Teams.Plugins.AspNetCore.DevTools.Controllers;

[ApiController]
[Obsolete("Use Minimal APIs instead.")]
public class ActivityController : ControllerBase
{
    private readonly DevToolsPlugin _plugin;
    private readonly SecurityKey _securityKey;

    public ActivityController(DevToolsPlugin plugin)
    {
        _plugin = plugin;
        _securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("secret_devtools_signing_key_123456"));
    }

    [HttpPost("/v3/conversations/{conversationId}/activities")]
    public async Task<IResult> Create(string conversationId, [FromBody] JsonNode body, CancellationToken cancellationToken)
    {
        var isClient = HttpContext.Request.Headers.TryGetValue("x-teams-devtools", out var strings) && strings.Any(h => h == "true");
        body["id"] ??= Guid.NewGuid().ToString();

        if (!isClient)
        {
            return Results.Json(new { id = body["id"] }, statusCode: 201);
        }

        body["from"] ??= JsonSerializer.SerializeToNode(new Account()
        {
            Id = "devtools",
            Name = "devtools",
            Role = Role.User
        });

        body["conversation"] = JsonSerializer.SerializeToNode(new Conversation()
        {
            Id = conversationId,
            Type = ConversationType.Personal,
            Name = "default"
        });

        body["recipient"] = JsonSerializer.SerializeToNode(new Account()
        {
            Id = _plugin.AppId ?? string.Empty,
            Name = _plugin.AppName,
            Role = Role.Bot
        });

        var activity = JsonSerializer.Deserialize<Activity>(JsonSerializer.Serialize(body));

        if (activity is null)
        {
            return Results.BadRequest();
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Claims = new Dictionary<string, object>()
            {
                { "serviceurl", $"http://localhost:{HttpContext.Request.Host.Port}/" }
            },
            SigningCredentials = new SigningCredentials(_securityKey, SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new IdentityModel.JsonWebTokens.JsonWebTokenHandler
        {
            SetDefaultTimesOnTokenCreation = false
        };

        var tokenString = tokenHandler.CreateToken(descriptor);
        var token = new JsonWebToken(tokenString);
        var _ = await _plugin.Do(new()
        {
            Token = token,
            Activity = activity,
            Services = HttpContext.RequestServices.CreateAsyncScope().ServiceProvider,
        }, cancellationToken);

        return Results.Json(new { id = body["id"] }, statusCode: 201);
    }
}