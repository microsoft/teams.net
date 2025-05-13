using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

public static partial class Tab
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class FetchAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.Tabs.Fetch, typeof(Tabs.FetchActivity))
    {
        public override object Coerce(IContext<IActivity> context) => context.ToActivityType<Tabs.FetchActivity>();
    }
}

public static partial class AppInvokeActivityExtensions
{
    public static App OnTabFetch(this App app, Func<IContext<Tabs.FetchActivity>, Task<object?>> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = context => handler(context.ToActivityType<Tabs.FetchActivity>()),
            Selector = activity =>
            {
                if (activity is Tabs.FetchActivity)
                {
                    return true;
                }

                return false;
            }
        });

        return app;
    }
}