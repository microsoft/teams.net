// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Schema;

IConfiguration configuration = new ConfigurationBuilder()
           .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
           .AddJsonFile("appsettings.json")
           .AddEnvironmentVariables()
           .Build();

ServiceCollection services = new ServiceCollection();
services.AddSingleton(configuration);
services.AddLogging(c => {
    c.AddConfiguration(configuration);
    c.AddConsole();
});
services.AddTeamsBotApplication();
var provider = services.BuildServiceProvider();
var teamsBotApplication = provider.GetRequiredService<TeamsBotApplication>();
Console.WriteLine($"Running Teams Bot Application for appId '{teamsBotApplication.AppId}' with version '{TeamsBotApplication.Version}'.");


var smba = new Uri("https://smba.trafficmanager.net/amer");
var membersClient = teamsBotApplication.Api.ForServiceUrl(smba).Conversations.Members;

string cid = "19%3ALydFnezGKSkhYoiLNP6kZ8AuXQr36EDAkvG9CNJSPKc1%40thread.tacv2";
var paged = await membersClient.GetPagedAsync(cid, 52);

List<TeamsConversationAccount?> members = [..paged.Members];

while (!string.IsNullOrEmpty(paged.ContinuationToken))
{
    Console.WriteLine("Getting next page of members...");
    paged = await membersClient.GetPagedAsync(cid, 52, paged.ContinuationToken);
    members.AddRange(paged.Members);
}

Console.WriteLine(members.Count);
