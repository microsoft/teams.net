


using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Activities;


var builder = WebApplication.CreateBuilder(args);
builder.AddTeams();
var app = builder.Build();
var teamsApp = app.UseTeams();

teamsApp.OnMessage(context =>
{
    return context.Send("you said: " + context.Activity.Text);
});

teamsApp.OnMessageReaction(context =>
{
    return context.Send("you reacted to a message " + context.Activity.ReactionsAdded!.First().Type.ToString());
});

app.Run();