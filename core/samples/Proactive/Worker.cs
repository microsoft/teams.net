// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Schema;

namespace Proactive;

public class Worker(ConversationClient conversationClient, ILogger<Worker> logger) : BackgroundService
{
    private const string ConversationId = "a:17vxw6pGQOb3Zfh8acXT8m_PqHycYpaFgzu2mFMUfkT-h0UskMctq5ZPPc7FIQxn2bx7rBSm5yE_HeUXsCcKZBrv77RgorB3_1_pAdvMhi39ClxQgawzyQ9GBFkdiwOxT";
    private const string ServiceUrl = "https://smba.trafficmanager.net/teams/";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                CoreActivity proactiveMessage = CoreActivity.CreateBuilder()
                    .Build();
                proactiveMessage.Properties["text"] = $"Proactive hello at {DateTimeOffset.Now}";
                SendActivityResponse? aid = await conversationClient.SendActivityAsync(ConversationId, proactiveMessage, new Uri(ServiceUrl), cancellationToken: stoppingToken);
                logger.LogInformation("Activity {Aid} sent", aid?.Id ?? "unknown");
            }
            await Task.Delay(1000, stoppingToken);
        }
    }
}
