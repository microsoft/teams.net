using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

public static partial class Config
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class FetchAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.Configs.Fetch, typeof(Configs.FetchActivity))
    {
        public override object Coerce(IContext<IActivity> context) => context.ToActivityType<Configs.FetchActivity>();
    }
}

public static partial class AppInvokeActivityExtensions
{
    public static App OnConfigFetch(this App app, Func<IContext<Configs.FetchActivity>, Task<object?>> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = context => handler(context.ToActivityType<Configs.FetchActivity>()),
            Selector = activity => activity is Configs.FetchActivity
        });

        return app;
    }
}