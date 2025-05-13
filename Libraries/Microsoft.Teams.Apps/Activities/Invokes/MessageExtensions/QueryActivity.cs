using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

public static partial class MessageExtension
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class QueryAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.MessageExtensions.Query, typeof(MessageExtensions.QueryActivity))
    {
        public override object Coerce(IContext<IActivity> context) => context.ToActivityType<MessageExtensions.QueryActivity>();
    }
}

public static partial class AppInvokeActivityExtensions
{
    public static App OnQuery(this App app, Func<IContext<MessageExtensions.QueryActivity>, Task<object?>> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = context => handler(context.ToActivityType<MessageExtensions.QueryActivity>()),
            Selector = activity =>
            {
                if (activity is MessageExtensions.QueryActivity)
                {
                    return true;
                }

                return false;
            }
        });

        return app;
    }
}