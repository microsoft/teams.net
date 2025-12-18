// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Core;
using Microsoft.Bot.Core.Schema;

namespace Proactive;

public class Worker(ConversationClient conversationClient, ILogger<Worker> logger) : BackgroundService
{
    private const string ConversationId = "a:17vxw6pGQOb3Zfh8acXT8m_PqHycYpaFgzu2mFMUfkT-h0UskMctq5ZPPc7FIQxn2bx7rBSm5yE_HeUXsCcKZBrv77RgorB3_1_pAdvMhi39ClxQgawzyQ9GBFkdiwOxT";
    private const string FromId = "28:56653e9d-2158-46ee-90d7-675c39642038";
    private const string ServiceUrl = "https://smba.trafficmanager.net/teams/";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                CoreActivity proactiveMessage = new()
                {
                    Text = $"Proactive hello at {DateTimeOffset.Now}",
                    ServiceUrl = new Uri(ServiceUrl),
                    From = new() { Id = FromId },
                    Conversation = new() { Id = ConversationId }
                };
                var aid = await conversationClient.SendActivityAsync(proactiveMessage, stoppingToken);
                logger.LogInformation("Activity {Aid} sent", aid.Id);
            }
            await Task.Delay(1000, stoppingToken);
        }
    }
}
