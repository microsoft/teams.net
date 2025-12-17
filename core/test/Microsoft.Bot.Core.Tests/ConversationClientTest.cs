// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Core.Hosting;
using Microsoft.Bot.Core.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Bot.Core;

namespace Microsoft.Bot.Core.Tests;

public class ConversationClientTest
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ConversationClient _conversationClient;
    private readonly Uri _serviceUrl;

    public ConversationClientTest()
    {
        IConfigurationBuilder builder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddEnvironmentVariables();

        IConfiguration configuration = builder.Build();

        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton(configuration);
        services.AddBotApplication<BotApplication>();
        _serviceProvider = services.BuildServiceProvider();
        _conversationClient = _serviceProvider.GetRequiredService<ConversationClient>();
        _serviceUrl = new Uri(Environment.GetEnvironmentVariable("TEST_SERVICEURL") ?? "https://smba.trafficmanager.net/teams/");
    }

    [Fact]
    public async Task SendActivityDefault()
    {
        CoreActivity activity = new()
        {
            Type = ActivityTypes.Message,
            Text = $"Message from Automated tests, running in SDK `{BotApplication.Version}` at `{DateTime.UtcNow:s}`",
            ServiceUrl = _serviceUrl,
            Conversation = new()
            {
                Id = Environment.GetEnvironmentVariable("TEST_CONVERSATIONID") ?? throw new InvalidOperationException("TEST_ConversationId environment variable not set")
            }
        };
        ResourceResponse res = await _conversationClient.SendActivityAsync(activity, CancellationToken.None);
        Assert.NotNull(res);
        Assert.NotNull(res.Id);
    }


    [Fact]
    public async Task SendActivityToChannel()
    {
        CoreActivity activity = new()
        {
            Type = ActivityTypes.Message,
            Text = $"Message from Automated tests, running in SDK `{BotApplication.Version}` at `{DateTime.UtcNow:s}`",
            ServiceUrl = _serviceUrl,
            Conversation = new()
            {
                Id = Environment.GetEnvironmentVariable("TEST_CHANNELID") ?? throw new InvalidOperationException("TEST_CHANNELID environment variable not set")
            }
        };
        ResourceResponse res = await _conversationClient.SendActivityAsync(activity, CancellationToken.None);
        Assert.NotNull(res);
        Assert.NotNull(res.Id);
    }

    [Fact]
    public async Task SendActivityToPersonalChat_FailsWithBad_ConversationId()
    {
        CoreActivity activity = new()
        {
            Type = ActivityTypes.Message,
            Text = $"Message from Automated tests, running in SDK `{BotApplication.Version}` at `{DateTime.UtcNow:s}`",
            ServiceUrl = _serviceUrl,
            Conversation = new()
            {
                Id = "a:1"
            }
        };

        await Assert.ThrowsAsync<HttpRequestException>(()
            => _conversationClient.SendActivityAsync(activity));
    }

    [Fact]
    public async Task UpdateActivity()
    {
        // First send an activity to get an ID
        CoreActivity activity = new()
        {
            Type = ActivityTypes.Message,
            Text = $"Original message from Automated tests at `{DateTime.UtcNow:s}`",
            ServiceUrl = _serviceUrl,
            Conversation = new()
            {
                Id = Environment.GetEnvironmentVariable("TEST_CONVERSATIONID") ?? throw new InvalidOperationException("TEST_ConversationId environment variable not set")
            }
        };

        ResourceResponse sendResponse = await _conversationClient.SendActivityAsync(activity, CancellationToken.None);
        Assert.NotNull(sendResponse);
        Assert.NotNull(sendResponse.Id);

        // Now update the activity
        CoreActivity updatedActivity = new()
        {
            Type = ActivityTypes.Message,
            Text = $"Updated message from Automated tests at `{DateTime.UtcNow:s}`",
            ServiceUrl = _serviceUrl,
        };

        ResourceResponse updateResponse = await _conversationClient.UpdateActivityAsync(
            activity.Conversation.Id,
            sendResponse.Id,
            updatedActivity,
            CancellationToken.None);

        Assert.NotNull(updateResponse);
        Assert.NotNull(updateResponse.Id);
    }

    [Fact]
    public async Task DeleteActivity()
    {
        // First send an activity to get an ID
        CoreActivity activity = new()
        {
            Type = ActivityTypes.Message,
            Text = $"Message to delete from Automated tests at `{DateTime.UtcNow:s}`",
            ServiceUrl = _serviceUrl,
            Conversation = new()
            {
                Id = Environment.GetEnvironmentVariable("TEST_CONVERSATIONID") ?? throw new InvalidOperationException("TEST_ConversationId environment variable not set")
            }
        };

        ResourceResponse sendResponse = await _conversationClient.SendActivityAsync(activity, CancellationToken.None);
        Assert.NotNull(sendResponse);
        Assert.NotNull(sendResponse.Id);

        // Add a delay for 5 seconds
        await Task.Delay(TimeSpan.FromSeconds(5));

        // Now delete the activity
        await _conversationClient.DeleteActivityAsync(
            activity.Conversation.Id,
            sendResponse.Id,
            _serviceUrl,
            cancellationToken: CancellationToken.None);

        // If no exception was thrown, the delete was successful
    }
}
