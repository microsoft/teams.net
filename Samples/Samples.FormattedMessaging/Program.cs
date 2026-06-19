using Microsoft.Teams.Api;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

using MessageActivity = Microsoft.Teams.Api.Activities.MessageActivity;

var builder = WebApplication.CreateBuilder(args);
builder.AddTeams();
var app = builder.Build();
var teams = app.UseTeams();

teams.OnMessage(async (context, cancellationToken) =>
{
    await context.Typing("processing your response", cancellationToken);
    var text = context.Activity.Text?.ToLowerInvariant() ?? "";

    if (text.Contains("extended"))
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
    }
    else if (text.Contains("markdown"))
    {
        var reply = new MessageActivity("""
# Markdown Demo

**Bold**, *italic*, and ~~strikethrough~~

- Item one
- Item two
- Item three

> This is a blockquote

`inline code` and [a link](https://www.microsoft.com)
""")
        {
            TextFormat = TextFormat.Markdown
        };
        await context.Send(reply, cancellationToken);
    }
    else if (text.Contains("xml"))
    {
        var reply = new MessageActivity(
            "<b>Bold</b>, <i>italic</i>, and <strike>strikethrough</strike><br/>" +
            "<ul><li>Item one</li><li>Item two</li><li>Item three</li></ul>")
        {
            TextFormat = TextFormat.Xml
        };
        await context.Send(reply, cancellationToken);
    }
    else if (text.Contains("plain"))
    {
        var reply = new MessageActivity("This is plain text with no formatting applied.")
        {
            TextFormat = TextFormat.Plain
        };
        await context.Send(reply, cancellationToken);
    }
    else
    {
        await context.Send(
            "Send **markdown**, **extended**, **xml**, or **plain** to see different text formats.",
            cancellationToken);
    }
});

app.Run();
