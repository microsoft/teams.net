using Microsoft.Teams.Api.Activities;

namespace Microsoft.Teams.Apps.Routing;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class TypingAttribute() : ActivityAttribute(ActivityType.Typing, typeof(TypingActivity))
{
    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<TypingActivity>();
}

public partial interface IRoutingModule
{
    public IRoutingModule OnTyping(Func<IContext<TypingActivity>, Task> handler);
}

public partial class RoutingModule : IRoutingModule
{
    public IRoutingModule OnTyping(Func<IContext<TypingActivity>, Task> handler)
    {
        Router.Register(new Route()
        {
            Handler = async context =>
            {
                await handler(context.ToActivityType<TypingActivity>());
                return null;
            },
            Selector = activity =>
            {
                if (activity is TypingActivity typing)
                {
                    return true;
                }

                return false;
            }
        });

        return this;
    }
}