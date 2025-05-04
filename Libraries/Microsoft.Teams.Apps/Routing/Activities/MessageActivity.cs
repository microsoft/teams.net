using System.Text.RegularExpressions;

using Microsoft.Teams.Api.Activities;

namespace Microsoft.Teams.Apps.Routing;

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

public partial interface IRoutingModule
{
    public IRoutingModule OnMessage(Func<IContext<MessageActivity>, Task> handler);
    public IRoutingModule OnMessage(string pattern, Func<IContext<MessageActivity>, Task> handler);
    public IRoutingModule OnMessage(Regex pattern, Func<IContext<MessageActivity>, Task> handler);
}

public partial class RoutingModule : IRoutingModule
{
    public IRoutingModule OnMessage(Func<IContext<MessageActivity>, Task> handler)
    {
        Router.Register(new Route()
        {
            Handler = async context =>
            {
                await handler(context.ToActivityType<MessageActivity>());
                return null;
            },
            Selector = activity =>
            {
                if (activity is MessageActivity message)
                {
                    return true;
                }

                return false;
            }
        });

        return this;
    }

    public IRoutingModule OnMessage(string pattern, Func<IContext<MessageActivity>, Task> handler)
    {
        Router.Register(new Route()
        {
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

        return this;
    }

    public IRoutingModule OnMessage(Regex regex, Func<IContext<MessageActivity>, Task> handler)
    {
        Router.Register(new Route()
        {
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

        return this;
    }
}