using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Events;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Annotations;
using Microsoft.Teams.Cards;
using Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

namespace Samples.Meetings;

public static partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddOpenApi();
        builder.Services.AddTransient<Controller>();
        builder.AddTeams().AddTeamsDevTools();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        app.UseTeams();
        app.Run();
    }

    [TeamsController]
    public class Controller
    {
        [Microsoft.Teams.Apps.Activities.Events.Event.MeetingStart]
        public async Task OnMeetingStart(
            IContext<MeetingStartActivity> context,
            [Context] IContext.Client client,
            [Context] IContext.Next next)
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

            await client.Send(card);
        }

        [Microsoft.Teams.Apps.Activities.Events.Event.MeetingEnd]
        public async Task OnMeetingEnd(
            IContext<MeetingEndActivity> context,
            [Context] IContext.Client client,
            [Context] IContext.Next next)
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

            await client.Send(card);
        }

        [Microsoft.Teams.Apps.Activities.Events.Event.MeetingJoin]
        public async Task OnMeetingParticipantJoin(
            IContext<MeetingParticipantJoinActivity> context,
            [Context] IContext.Client client,
            [Context] IContext.Next next)
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

            await client.Send(card);
        }

        [Microsoft.Teams.Apps.Activities.Events.Event.MeetingLeave]
        public async Task OnMeetingParticipantLeave(
            IContext<MeetingParticipantLeaveActivity> context,
            [Context] IContext.Client client,
            [Context] IContext.Next next)
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

            await client.Send(card);
        }

        [Message]
        public async Task OnMessage([Context] MessageActivity activity, [Context] IContext.Client client)
        {
            await client.Typing();
            await client.Send($"you said '{activity.Text}'");
        }
    }
}