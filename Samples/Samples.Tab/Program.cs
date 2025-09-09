using Microsoft.Teams.Extensions.Logging;
using Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.AddTeams().AddTeamsDevTools();

var app = builder.Build();

app.UseTeams();
app.AddTab("test", "Web/bin");
app.AddFunction<Samples.Tab.Body>("post-to-chat", async context =>
{
    await context.Send(context.Data.Message);
    return new Dictionary<string, object?>()
    {
        { "conversationId", context.ConversationId },
    };
});

app.Run();