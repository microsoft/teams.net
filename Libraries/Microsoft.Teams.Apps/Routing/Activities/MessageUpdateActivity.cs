using Microsoft.Teams.Api.Activities;

namespace Microsoft.Teams.Apps.Routing;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class MessageUpdateAttribute() : ActivityAttribute(ActivityType.MessageUpdate, typeof(MessageUpdateActivity))
{
    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<MessageUpdateActivity>();
}

public partial interface IRoutingModule
{
    public IRoutingModule OnMessageUpdate(Func<IContext<MessageUpdateActivity>, Task> handler);
}

public partial class RoutingModule : IRoutingModule
{
    public IRoutingModule OnMessageUpdate(Func<IContext<MessageUpdateActivity>, Task> handler)
    {
        Router.Register(new Route()
        {
            Handler = async context =>
            {
                await handler(context.ToActivityType<MessageUpdateActivity>());
                return null;
            },
            Selector = activity =>
            {
                if (activity is MessageUpdateActivity messageUpdate)
                {
                    return true;
                }

                return false;
            }
        });

        return this;
    }
}