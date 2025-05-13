using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

public static partial class MessageExtension
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class CardButtonClickedAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.MessageExtensions.CardButtonClicked, typeof(MessageExtensions.CardButtonClickedActivity))
    {
        public override object Coerce(IContext<IActivity> context) => context.ToActivityType<MessageExtensions.CardButtonClickedActivity>();
    }
}

public static partial class AppInvokeActivityExtensions
{
    public static App OnCardButtonClicked(this App app, Func<IContext<MessageExtensions.CardButtonClickedActivity>, Task<object?>> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = context => handler(context.ToActivityType<MessageExtensions.CardButtonClickedActivity>()),
            Selector = activity =>
            {
                if (activity is MessageExtensions.CardButtonClickedActivity)
                {
                    return true;
                }

                return false;
            }
        });

        return app;
    }
}