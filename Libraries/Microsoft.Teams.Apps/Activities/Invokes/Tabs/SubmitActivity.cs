using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

public static partial class Tab
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class SubmitAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.Tabs.Submit, typeof(Tabs.SubmitActivity))
    {
        public override object Coerce(IContext<IActivity> context) => context.ToActivityType<Tabs.SubmitActivity>();
    }
}

public static partial class AppInvokeActivityExtensions
{
    public static App OnTabSubmit(this App app, Func<IContext<Tabs.SubmitActivity>, Task<object?>> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = context => handler(context.ToActivityType<Tabs.SubmitActivity>()),
            Selector = activity =>
            {
                if (activity is Tabs.SubmitActivity)
                {
                    return true;
                }

                return false;
            }
        });

        return app;
    }
}