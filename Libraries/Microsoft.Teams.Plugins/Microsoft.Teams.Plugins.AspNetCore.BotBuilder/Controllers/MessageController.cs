// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;

namespace Microsoft.Teams.Plugins.AspNetCore.BotBuilder
{
    [ApiController]
    public class MessageController : ControllerBase
    {
        private readonly IBotFrameworkHttpAdapter _adapter;
        private readonly IBot _bot;

        private readonly AspNetCorePlugin _plugin;

        private readonly IHostApplicationLifetime _lifetime;

        public MessageController(IBotFrameworkHttpAdapter adapter, IBot bot, AspNetCorePlugin plugin, IHostApplicationLifetime lifetime)
        {
            _plugin = plugin;
            _lifetime = lifetime;
            _adapter = adapter;
            _bot = bot;
        }

        [HttpPost("/api/messages")]
        public async Task<IResult> PostAsync()
        {
            HttpContext.Request.EnableBuffering();
            var body = await new StreamReader(Request.Body).ReadToEndAsync();
            Activity? activity = JsonSerializer.Deserialize<Activity>(body);
            HttpContext.Request.Body.Position = 0;

            if (activity == null)
            {
                return Results.BadRequest("Missing activity");
            }

            // Delegate the processing of the HTTP POST to the adapter.
            // The adapter will invoke the bot.
            await _adapter.ProcessAsync(HttpContext.Request, HttpContext.Response, _bot);

            if (Response.HasStarted)
            {
                return Results.Empty;
            }

            // Fallback logic
            var authHeader = HttpContext.Request.Headers.Authorization.FirstOrDefault() ?? throw new UnauthorizedAccessException();
            var token = new JsonWebToken(authHeader.Replace("Bearer ", ""));
            var res = await _plugin.Do(new()
            {
                Token = token,
                Activity = activity,
                Services = HttpContext.RequestServices
            }, _lifetime.ApplicationStopping);

            return Results.Json(res.Body, statusCode: (int)res.Status);
        }
    }
}