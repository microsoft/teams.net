using Microsoft.Teams.Api.Clients;
using Microsoft.Teams.Api.Messages;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.AddTeams().AddTeamsDevTools();
var app = builder.Build();
var teams = app.UseTeams();


teams.OnMessage(async (context, cancellationToken) =>
{
    await context.Send($"you said '{context.Activity.Text}'", cancellationToken);


    // replace with context.Api.Conversations.Reactions once Reactions client is available in PROD.
    var api = new ApiClient(context.Activity.ServiceUrl!, context.Api.Client, cancellationToken);
        
    await api.Conversations.Reactions.AddAsync(context.Activity.Conversation.Id, context.Activity.Id, new ReactionType("1f44b_wavinghand-tone4"));

    await Task.Delay(2000, cancellationToken);
    await api.Conversations.Reactions.AddAsync(context.Activity.Conversation.Id, context.Activity.Id, new ReactionType("1f601_beamingfacewithsmilingeyes"));
    
    await Task.Delay(2000, cancellationToken);
    await api.Conversations.Reactions.DeleteAsync(context.Activity.Conversation.Id, context.Activity.Id, new ReactionType("1f601_beamingfacewithsmilingeyes"));
    
});

teams.OnMessageReaction(async (context, cancellationToken) =>
{
    context.Log.Info($"Reaction '{context.Activity.ReactionsAdded?.FirstOrDefault()?.Type}' added by {context.Activity.From?.Name}");
    await context.Send($"you added '{context.Activity.ReactionsAdded?.FirstOrDefault()?.Type}' " +
        $"and removed '{context.Activity.ReactionsRemoved?.FirstOrDefault()?.Type}'", cancellationToken);
});

app.Run();