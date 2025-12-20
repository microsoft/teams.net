// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Core;
using Microsoft.Bot.Core.Hosting;
using Microsoft.Bot.Core.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


string ConversationId = "a:17vxw6pGQOb3Zfh8acXT8m_PqHycYpaFgzu2mFMUfkT-h0UskMctq5ZPPc7FIQxn2bx7rBSm5yE_HeUXsCcKZBrv77RgorB3_1_pAdvMhi39ClxQgawzyQ9GBFkdiwOxT";
string FromId = "28:56653e9d-2158-46ee-90d7-675c39642038";
string ServiceUrl = "https://smba.trafficmanager.net/teams/";

ConversationClient conversationClient = CreateConversationClient();
await conversationClient.SendActivityAsync(new CoreActivity
{
    //Text = "Hello from MSAL Config API test!",
    Conversation = new() { Id = ConversationId },
    ServiceUrl = new Uri(ServiceUrl),
    From = new() { Id = FromId }

}, cancellationToken: default);

await conversationClient.SendActivityAsync(new CoreActivity
{
    //Text = "Hello from MSAL Config API test!",
    Conversation = new() { Id = "bad conversation" },
    ServiceUrl = new Uri(ServiceUrl),
    From = new() { Id = FromId }

}, cancellationToken: default);



static ConversationClient CreateConversationClient()
{
    ServiceCollection services = InitializeDIContainer();
    services.AddConversationClient();
    ServiceProvider serviceProvider = services.BuildServiceProvider();
    ConversationClient conversationClient = serviceProvider.GetRequiredService<ConversationClient>();
    return conversationClient;
}

static ServiceCollection InitializeDIContainer()
{
    IConfigurationBuilder builder = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddEnvironmentVariables();

    IConfiguration configuration = builder.Build();

    ServiceCollection services = new();
    services.AddSingleton(configuration);
    services.AddLogging(configure => configure.AddConsole());
    return services;
}
