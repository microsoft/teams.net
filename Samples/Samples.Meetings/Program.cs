using System.Diagnostics;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Events;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Activities.Events;
using Microsoft.Teams.Apps.Annotations;
using Microsoft.Teams.Cards;
using Microsoft.Teams.Common.Logging;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

var appBuilder = App.Builder()
    .AddLogger(new ConsoleLogger(level: Microsoft.Teams.Common.Logging.LogLevel.Debug));

builder.AddTeams(appBuilder);

var app = builder.Build();
var teams = app.UseTeams();

teams.Use(async context =>
{
    var start = DateTime.UtcNow;
    try
    {
        await context.Next();
    }
    catch(Exception e)
    {
        context.Log.Error(e);
        context.Log.Error("error occurred during activity processing");
    }
    context.Log.Debug($"request took {(DateTime.UtcNow - start).TotalMilliseconds}ms");
});

teams.OnMeetingStart(async context =>
{
    var activity = context.Activity.Value;
    var startTime = activity.StartTime.ToLocalTime();
    AdaptiveCard card = new AdaptiveCard
    {
        Schema = "http://adaptivecards.io/schemas/adaptive-card.json",
        Body = new List<CardElement>
        {
            new TextBlock($"'{activity.Title}' has started at {startTime}.")
            {
                Wrap = true,
                Weight = TextWeight.Bolder
            }
        },
        Actions = new List<Microsoft.Teams.Cards.Action>
        {
            new OpenUrlAction(activity.JoinUrl)
            {
               Title = "Join the meeting",
            }
        }
    };
    await context.Send(card);
});

teams.OnMeetingEnd(async context =>
{
    var activity = context.Activity.Value;
    var endTime = activity.EndTime.ToLocalTime();

    AdaptiveCard card = new AdaptiveCard
    {
        Schema = "http://adaptivecards.io/schemas/adaptive-card.json",
        Body = new List<CardElement>
                {
                    new TextBlock($"'{activity.Title}' has ended at {endTime}.")
                    {
                        Wrap = true,
                        Weight = TextWeight.Bolder
                    }
                }
    };

    await context.Send(card);
});

teams.OnMeetingJoin(async context =>
{
    var activity = context.Activity.Value;
    var member = activity.Members[0].User.Name;
    var role = activity.Members[0].Meeting.Role;

    AdaptiveCard card = new AdaptiveCard
    {
        Schema = "http://adaptivecards.io/schemas/adaptive-card.json",
        Body = new List<CardElement>
                {
                    new TextBlock($"{member} has joined the meeting as {role}.")
                    {
                        Wrap = true,
                        Weight = TextWeight.Bolder
                    }
                }
    };

    await context.Send(card);

});

teams.OnMeetingLeave(async context =>
{
    var activity = context.Activity.Value;
    var member = activity.Members[0].User.Name;

    AdaptiveCard card = new AdaptiveCard
    {
        Schema = "http://adaptivecards.io/schemas/adaptive-card.json",
        Body = new List<CardElement>
                {
                    new TextBlock($"{member} has left the meeting.")
                    {
                        Wrap = true,
                        Weight = TextWeight.Bolder
                    }
                }
    };

    await context.Send(card);
});

teams.OnMessage(async context =>
{
    await context.Typing();
    await context.Send($"you said '{context.Activity.Text}'");
});

app.Run();