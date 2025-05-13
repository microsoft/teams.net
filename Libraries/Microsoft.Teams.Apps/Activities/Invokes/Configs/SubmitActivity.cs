using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

public static partial class Config
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class SubmitAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.Configs.Submit, typeof(Configs.SubmitActivity))
    {
        public override object Coerce(IContext<IActivity> context) => context.ToActivityType<Configs.SubmitActivity>();
    }
}

public static partial class AppInvokeActivityExtensions
{
    public static App OnConfigSubmit(this App app, Func<IContext<Configs.SubmitActivity>, Task<object?>> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = context => handler(context.ToActivityType<Configs.SubmitActivity>()),
            Selector = activity =>
            {
                if (activity is Configs.SubmitActivity)
                {
                    return true;
                }

                return false;
            }
        });

        return app;
    }
}