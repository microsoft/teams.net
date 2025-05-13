using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

public static partial class MessageExtension
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class QueryLinkAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.MessageExtensions.QueryLink, typeof(MessageExtensions.QueryLinkActivity))
    {
        public override object Coerce(IContext<IActivity> context) => context.ToActivityType<MessageExtensions.QueryLinkActivity>();
    }
}

public static partial class AppInvokeActivityExtensions
{
    public static App OnQueryLink(this App app, Func<IContext<MessageExtensions.QueryLinkActivity>, Task<object?>> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = context => handler(context.ToActivityType<MessageExtensions.QueryLinkActivity>()),
            Selector = activity =>
            {
                if (activity is MessageExtensions.QueryLinkActivity)
                {
                    return true;
                }

                return false;
            }
        });

        return app;
    }
}