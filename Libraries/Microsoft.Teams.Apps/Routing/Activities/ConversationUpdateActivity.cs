using Microsoft.Teams.Api.Activities;

namespace Microsoft.Teams.Apps.Routing;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class ConversationUpdateAttribute() : ActivityAttribute(ActivityType.ConversationUpdate, typeof(ConversationUpdateActivity))
{
    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<ConversationUpdateActivity>();
}

public partial interface IRoutingModule
{
    public IRoutingModule OnConversationUpdate(Func<IContext<ConversationUpdateActivity>, Task> handler);
}

public partial class RoutingModule : IRoutingModule
{
    public IRoutingModule OnConversationUpdate(Func<IContext<ConversationUpdateActivity>, Task> handler)
    {
        Router.Register(new Route()
        {
            Handler = async context =>
            {
                await handler(context.ToActivityType<ConversationUpdateActivity>());
                return null;
            },
            Selector = activity =>
            {
                if (activity is ConversationUpdateActivity conversationUpdate)
                {
                    return true;
                }

                return false;
            }
        });

        return this;
    }
}