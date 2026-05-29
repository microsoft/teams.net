using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.AddTeams();
var app = builder.Build();
var teams = app.UseTeams();

teams.OnActivity(async (context, cancellationToken) =>
{
    context.Log.Info(context.AppId);
    await context.Next();
});

teams.OnMessage(async (context, cancellationToken) =>
{
    context.Log.Info("hit!");
    await context.Typing("processing your response", cancellationToken);
    await context.Send($"you said '{context.Activity.Text}'", cancellationToken);


    var paged = await context.Api.Conversations.Members.GetPagedAsync(context.Activity.Conversation.Id, cancellationToken: cancellationToken);


    var first = paged.Members?.FirstOrDefault();

    await context.Send($"there are {paged.Members?.Count} members in this conversation. {first?.AadObjectId}", cancellationToken);


});

app.Run();