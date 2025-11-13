using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Common.Http;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder();
builder.Services.AddHttpClient();
builder.Services.AddTokenAcquisition();
builder.Services.AddInMemoryTokenCaches();

builder.Services.AddScoped<IHttpCredentials, ClientCredentials>();
builder.Services.AddSingleton<AppOptions>();
builder.Services.Configure<MicrosoftIdentityApplicationOptions>("AzureAd", builder.Configuration.GetSection("AzureAd"));
#pragma warning disable ASP0000 // Use 'new(...)'
AppBuilder appBuilder = new AppBuilder(builder.Services.BuildServiceProvider());
#pragma warning restore ASP0000 // Use 'new(...)'


builder.AddTeams(appBuilder);
var app = builder.Build();
var teamsApp = app.UseTeams();

teamsApp.OnMessage(async context =>
{
    await context.Typing();
    await context.Send($"you said '{context.Activity.Text}'");
});

app.Run();
