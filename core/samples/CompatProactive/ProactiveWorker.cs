// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Bot.Compat;

namespace CompatProactive;

internal class ProactiveWorker(IBotFrameworkHttpAdapter compatAdapter, ILogger<ProactiveWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ConversationReference conversationReference = new()
        {
            ServiceUrl = "https://smba.trafficmanager.net/teams/",
            Conversation = new() { Id = "19:ad37a1f8af5549e3b81edf249fe5cb1b@thread.tacv2" },
        };

        await ((CompatAdapter)compatAdapter).ContinueConversationAsync("", conversationReference, callback, stoppingToken);
        logger.LogInformation("Proactive message sent");
    }

    private async Task callback(ITurnContext turnContext, CancellationToken cancellationToken)
    {
        await turnContext.SendActivitiesAsync(new Activity[]
        {
            MessageFactory.Text($"Proactive with Compat Layer {DateTimeOffset.Now}")
        }, cancellationToken);
    }
}
