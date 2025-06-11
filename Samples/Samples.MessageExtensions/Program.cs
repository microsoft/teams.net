
using Microsoft.Teams.Api.MessageExtensions;
using Microsoft.Teams.Apps.Activities.Invokes;
using Microsoft.Teams.Apps.Extensions;
using Microsoft.Teams.Cards;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

using Samples.MessageExtensions;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddTransient<HomeController>();
builder.AddTeams(Microsoft.Teams.Apps.App.Builder().AddLogger(level: Microsoft.Teams.Common.Logging.LogLevel.Debug));

var app = builder.Build();
var teams = app.UseTeams();


teams.OnQueryLink(async context =>
{
    context.Log.Info("OnQueryLink triggered:");
    var url = context.Activity.Value.Url;

    if (string.IsNullOrEmpty(url))
    {
        context.Log.Error("No URL provided for link unfurling.");
        return null;
    }

    var card = new Microsoft.Teams.Cards.AdaptiveCard
    {
        Body = new List<CardElement>
            {
                new TextBlock("Hello from Samples.Agent!")
                {
                    Size = TextSize.Large,
                    Weight = TextWeight.Bolder,
                    Color = TextColor.Accent,
                    Style = StyleEnum.Expanded,
                },
                new TextBlock(url) {
                    Size= TextSize.Small,
                    Weight = TextWeight.Lighter,
                    Color = TextColor.Good,
                }
            },
    };

    var response = new Response
    {
        ComposeExtension = new Result
        {
            Attachments = new List<Attachment>
               {
                    new Attachment()
                    {
                        Content = card,
                        Name = "Sample Card",
                    }
               },
            AttachmentLayout = Microsoft.Teams.Api.Attachment.Layout.List,
        }
    };

    return await Task.FromResult(response);
});

teams.OnMessageExtensionSubmitAction(async context =>
{
    context.Log.Info("OnSubmitAction triggered:");

    var commanndId = context.Activity.Value.CommandId;

    if (string.IsNullOrEmpty(commanndId))
    {
        context.Log.Error("No command ID provided for submit action.");
        return null;
    }

    if (commanndId == "createCard")
    {
        context.Log.Info("createCard response to submit action.");
        // todo: create card

    }
    else if (commanndId == "getMessageDetails" && context.Activity.Value.MessagePayload is not null)
    {
        context.Log.Info("getMessageDetails response to submit action.");
        // TODO: create message details card
    }
    else
    {
        context.Log.Error($"Unknown command ID: {commanndId}");
        return null;
    }
    return await Task.FromResult(new Response()
    {
        ComposeExtension = new Result()
        {
            Attachments = new List<Attachment>()
            {
                new Attachment()
                {
                    Content = "Your updated content here",
                    Name = "Your updated attachment name",
                }
            },
            AttachmentLayout = Microsoft.Teams.Api.Attachment.Layout.List,
        }
    });

});



teams.OnQuery(async context =>
{
    context.Log.Info("OnQuery triggered:");

    var query = context.Activity.Value;
    if (query.CommandId == "searchQuery")
    {
        var card = new Microsoft.Teams.Cards.AdaptiveCard
        {
            Body = new List<CardElement>
            {
                new TextBlock("Samples.Agent search query!")
                {
                    Size = TextSize.Large,
                    Weight = TextWeight.Bolder
                },
                new TextBlock("This is a sample card created with the CreateCard method.") {
                    Wrap = true
                }
            },
        };
        return await Task.FromResult(new Response()
        {
            ComposeExtension = new Result()
            {
                Attachments = new List<Attachment>()
            {
                new Attachment()
                {
                    Content = "Your updated content here",
                    Name = "Your updated attachment name",
                }
            },
                AttachmentLayout = Microsoft.Teams.Api.Attachment.Layout.List,
            }
        });
    }
    return null;

});

app.Run();