using Microsoft.Teams.Extensions.Logging;
using Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;
using Samples.Tab.Components;

var builder = WebApplication.CreateBuilder(args);
builder.AddTeams().AddTeamsDevTools();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();
app.UseTeams();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.AddFunction<Samples.Tab.Body>("post-to-chat", async context =>
{
    await context.Send(context.Data.Message);
    return new Dictionary<string, object?>()
    {
        { "conversationId", context.ConversationId },
    };
});

app.Run();