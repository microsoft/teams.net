// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.RegularExpressions;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class MessageAttribute : ActivityAttribute
{
    public Regex? Pattern { get; }

    public MessageAttribute() : base(ActivityType.Message, typeof(MessageActivity))
    {
    }

    public MessageAttribute(string pattern) : base(ActivityType.Message, typeof(MessageActivity))
    {
        Pattern = new Regex(pattern);
    }

    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<MessageActivity>();
    public override bool Select(IActivity activity)
    {
        if (activity is MessageActivity message)
        {
            return Pattern is null || Pattern.IsMatch(message.Text);
        }

        return false;
    }
}

public static partial class AppActivityExtensions
{
    public static App OnMessage(this App app, Func<IContext<MessageActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = ActivityType.Message,
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<MessageActivity>());
                return null;
            },
            Selector = activity => activity is MessageActivity
        });

        return app;
    }

    public static App OnMessage(this App app, string pattern, Func<IContext<MessageActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = ActivityType.Message,
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<MessageActivity>());
                return null;
            },
            Selector = activity =>
            {
                if (activity is MessageActivity message)
                {
                    return new Regex(pattern).IsMatch(message.Text);
                }

                return false;
            }
        });

        return app;
    }

    public static App OnMessage(this App app, Regex regex, Func<IContext<MessageActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = ActivityType.Message,
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<MessageActivity>());
                return null;
            },
            Selector = activity =>
            {
                if (activity is MessageActivity message)
                {
                    return regex.IsMatch(message.Text);
                }

                return false;
            }
        });

        return app;
    }
}