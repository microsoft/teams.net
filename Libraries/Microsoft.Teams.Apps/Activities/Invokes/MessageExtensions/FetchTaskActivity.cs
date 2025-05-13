using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

public static partial class MessageExtension
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class FetchTaskAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.MessageExtensions.FetchTask, typeof(MessageExtensions.FetchTaskActivity))
    {
        public override object Coerce(IContext<IActivity> context) => context.ToActivityType<MessageExtensions.FetchTaskActivity>();
    }
}

public static partial class AppInvokeActivityExtensions
{
    public static App OnFetchTask(this App app, Func<IContext<MessageExtensions.FetchTaskActivity>, Task<object?>> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = context => handler(context.ToActivityType<MessageExtensions.FetchTaskActivity>()),
            Selector = activity =>
            {
                if (activity is MessageExtensions.FetchTaskActivity)
                {
                    return true;
                }

                return false;
            }
        });

        return app;
    }
}