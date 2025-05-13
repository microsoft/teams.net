using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

public static partial class MessageExtension
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class AnonQueryLinkAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.MessageExtensions.AnonQueryLink, typeof(MessageExtensions.AnonQueryLinkActivity))
    {
        public override object Coerce(IContext<IActivity> context) => context.ToActivityType<MessageExtensions.AnonQueryLinkActivity>();
    }
}

public static partial class AppInvokeActivityExtensions
{
    public static App OnAnonQueryLink(this App app, Func<IContext<MessageExtensions.AnonQueryLinkActivity>, Task<object?>> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = context => handler(context.ToActivityType<MessageExtensions.AnonQueryLinkActivity>()),
            Selector = activity =>
            {
                if (activity is MessageExtensions.AnonQueryLinkActivity)
                {
                    return true;
                }

                return false;
            }
        });

        return app;
    }
}