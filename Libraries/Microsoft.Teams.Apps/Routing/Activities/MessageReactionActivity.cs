using Microsoft.Teams.Api.Activities;

namespace Microsoft.Teams.Apps.Routing;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class MessageReactionAttribute() : ActivityAttribute(ActivityType.MessageReaction, typeof(MessageReactionActivity))
{
    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<MessageReactionActivity>();
}

public partial interface IRoutingModule
{
    public IRoutingModule OnMessageReaction(Func<IContext<MessageReactionActivity>, Task> handler);
}

public partial class RoutingModule : IRoutingModule
{
    public IRoutingModule OnMessageReaction(Func<IContext<MessageReactionActivity>, Task> handler)
    {
        Router.Register(new Route()
        {
            Handler = async context =>
            {
                await handler(context.ToActivityType<MessageReactionActivity>());
                return null;
            },
            Selector = activity =>
            {
                if (activity is MessageReactionActivity messageReaction)
                {
                    return true;
                }

                return false;
            }
        });

        return this;
    }
}