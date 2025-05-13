using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class HandoffAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.Handoff, typeof(HandoffActivity))
{
    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<HandoffActivity>();
}

public static partial class AppInvokeActivityExtensions
{
    public static App OnHandoff(this App app, Func<IContext<HandoffActivity>, Task<object?>> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = context => handler(context.ToActivityType<HandoffActivity>()),
            Selector = activity => activity is HandoffActivity
        });

        return app;
    }
}