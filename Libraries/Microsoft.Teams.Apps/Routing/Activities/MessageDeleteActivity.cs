using Microsoft.Teams.Api.Activities;

namespace Microsoft.Teams.Apps.Routing;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class MessageDeleteAttribute() : ActivityAttribute(ActivityType.MessageDelete, typeof(MessageDeleteActivity))
{
    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<MessageDeleteActivity>();
}

public partial interface IRoutingModule
{
    public IRoutingModule OnMessageDelete(Func<IContext<MessageDeleteActivity>, Task> handler);
}

public partial class RoutingModule : IRoutingModule
{
    public IRoutingModule OnMessageDelete(Func<IContext<MessageDeleteActivity>, Task> handler)
    {
        Router.Register(new Route()
        {
            Handler = async context =>
            {
                await handler(context.ToActivityType<MessageDeleteActivity>());
                return null;
            },
            Selector = activity =>
            {
                if (activity is MessageDeleteActivity messageDelete)
                {
                    return true;
                }

                return false;
            }
        });

        return this;
    }
}