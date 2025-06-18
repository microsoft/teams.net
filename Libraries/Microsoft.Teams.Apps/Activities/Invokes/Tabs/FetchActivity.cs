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
    public static App OnTabFetch(this App app, Func<IContext<Tabs.FetchActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = async context =>
            {
                await handler(context.ToActivityType<Tabs.FetchActivity>());
                return null;
            },
            Selector = activity => activity is Tabs.FetchActivity
        });

        return app;
    }

    public static App OnTabFetch(this App app, Func<IContext<Tabs.FetchActivity>, Task<Response<Api.Tabs.Response>>> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = async context => await handler(context.ToActivityType<Tabs.FetchActivity>()),
            Selector = activity => activity is Tabs.FetchActivity
        });

        return app;
    }

    public static App OnTabFetch(this App app, Func<IContext<Tabs.FetchActivity>, Task<Api.Tabs.Response>> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = async context => await handler(context.ToActivityType<Tabs.FetchActivity>()),
            Selector = activity => activity is Tabs.FetchActivity
        });

        return app;
    }
}