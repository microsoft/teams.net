using Microsoft.Teams.Api.Activities;

namespace Microsoft.Teams.Apps.Routing;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class EventAttribute() : ActivityAttribute(ActivityType.Event, typeof(EventActivity))
{
    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<EventActivity>();
}

public partial interface IRoutingModule
{
    public IRoutingModule OnEvent(Func<IContext<EventActivity>, Task<object?>> handler);
}

public partial class RoutingModule : IRoutingModule
{
    public IRoutingModule OnEvent(Func<IContext<EventActivity>, Task<object?>> handler)
    {
        Router.Register(new Route()
        {
            Handler = context => handler(context.ToActivityType<EventActivity>()),
            Selector = activity =>
            {
                if (activity is EventActivity @event)
                {
                    return true;
                }

                return false;
            }
        });

        return this;
    }
}