using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

public static partial class AdaptiveCard
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class ActionAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.AdaptiveCards.Action, typeof(AdaptiveCards.ActionActivity))
    {
        public override object Coerce(IContext<IActivity> context) => context.ToActivityType<AdaptiveCards.ActionActivity>();
    }
}

public static partial class AppInvokeActivityExtensions
{
    public static App OnAdaptiveCardAction(this App app, Func<IContext<AdaptiveCards.ActionActivity>, Task<object?>> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = context => handler(context.ToActivityType<AdaptiveCards.ActionActivity>()),
            Selector = activity => activity is AdaptiveCards.ActionActivity
        });

        return app;
    }
}