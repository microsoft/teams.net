// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Extensions.Hosting;

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
            // Enable buffering so that the request can be read by the adapter and the plugin
            HttpContext.Request.EnableBuffering();

            // Delegate the processing of the HTTP POST to the adapter.
            // The adapter will invoke the bot.
            await _adapter.ProcessAsync(HttpContext.Request, HttpContext.Response, _bot);

            if (Response.HasStarted)
            {
                return Results.Empty;
            }

            // Fallback logic use the plugin to process the activity
            return await _plugin.Do(HttpContext, _lifetime.ApplicationStopping);
        }
    }
}