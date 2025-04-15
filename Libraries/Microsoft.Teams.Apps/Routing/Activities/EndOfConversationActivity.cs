using Microsoft.Teams.Api.Activities;

namespace Microsoft.Teams.Apps.Routing;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class EndOfConversationAttribute() : ActivityAttribute(ActivityType.EndOfConversation, typeof(EndOfConversationActivity))
{
    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<EndOfConversationActivity>();
}

public partial interface IRoutingModule
{
    public IRoutingModule OnEndOfConversation(Func<IContext<EndOfConversationActivity>, Task> handler);
}

public partial class RoutingModule : IRoutingModule
{
    public IRoutingModule OnEndOfConversation(Func<IContext<EndOfConversationActivity>, Task> handler)
    {
        Router.Register(new Route()
        {
            Handler = async context =>
            {
                await handler(context.ToActivityType<EndOfConversationActivity>());
                return null;
            },
            Selector = activity =>
            {
                if (activity is EndOfConversationActivity endOfConversation)
                {
                    return true;
                }

                return false;
            }
        });

        return this;
    }
}