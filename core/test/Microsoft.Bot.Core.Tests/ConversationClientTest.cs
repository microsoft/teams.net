using Microsoft.Bot.Core.Hosting;
using Microsoft.Bot.Core.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Bot.Core.Tests;

public class ConversationClientTest
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ConversationClient _conversationClient;

    public ConversationClientTest()
    {
        IConfigurationBuilder builder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddEnvironmentVariables();

        IConfiguration configuration = builder.Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddBotApplicationClients();
        _serviceProvider = services.BuildServiceProvider();
        _conversationClient = _serviceProvider.GetRequiredService<ConversationClient>();
        
    }

    [Fact]
    public async Task SendActivityDefault()
    {
        CoreActivity activity = new()
        {
            Type = ActivityTypes.Message,
            Text = $"Message from Automated tests, running in SDK `{BotApplication.Version}` at `{DateTime.UtcNow:s}`",
            ServiceUrl = new Uri("https://smba.trafficmanager.net/teams/"),
            Conversation = new()
            {
                Id = Environment.GetEnvironmentVariable("TEST_ConversationId") ?? throw new InvalidOperationException("TEST_ConversationId environment variable not set")
            }
        };
        var res = await _conversationClient.SendActivityAsync(activity, CancellationToken.None);
        Assert.NotNull(res);
        Assert.Contains("\"id\"", res);
    }

   

    [Fact]
    public async Task SendActivityToChannel()
    {
        CoreActivity activity = new()
        {
            Type = ActivityTypes.Message,
            Text = $"Message from Automated tests, running in SDK `{BotApplication.Version}` at `{DateTime.UtcNow:s}`",
            ServiceUrl = new Uri("https://smba.trafficmanager.net/teams/"),
            Conversation = new()
            {
                Id = "19:9f2af1bee7cc4a71af25ac72478fd5c6@thread.tacv2;messageid=1765420585482"
            }
        };
        var res = await _conversationClient.SendActivityAsync(activity, CancellationToken.None);
        Assert.NotNull(res);
        Assert.Contains("\"id\"", res);
    }

    [Fact]
    public async Task SendActivityToPersonalChat_FailsWithBad_ConversationId()
    {
        CoreActivity activity = new()
        {
            Type = ActivityTypes.Message,
            Text = $"Message from Automated tests, running in SDK `{BotApplication.Version}` at `{DateTime.UtcNow:s}`",
            ServiceUrl = new Uri("https://smba.trafficmanager.net/teams/"),
            Conversation = new()
            {
                Id = "a:1"
            }
        };

        await Assert.ThrowsAsync<HttpRequestException>(() 
            => _conversationClient.SendActivityAsync(activity));
    }
}
