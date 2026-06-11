using Microsoft.Teams.Api;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

using MessageActivity = Microsoft.Teams.Api.Activities.MessageActivity;

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

    if (context.Activity.Text?.Contains("extended", StringComparison.OrdinalIgnoreCase) == true)
    {
        var reply = new MessageActivity("""
# Extended Markdown Demo

## Table
| Feature | Status |
|---------|--------|
| Tables  | Supported |
| Math    | Supported |

## Math
$$E = mc^2$$
""")
        {
            TextFormat = TextFormat.ExtendedMarkdown
        };
        await context.Send(reply, cancellationToken);
        return;
    }

    await context.Send($"you said '{context.Activity.Text}'", cancellationToken);
});

app.Run();