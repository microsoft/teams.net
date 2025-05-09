using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Events;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Events;

public static partial class Event
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class ReadReceiptAttribute() : EventAttribute
    {
        public override object Coerce(IContext<IActivity> context) => context.ToActivityType<ReadReceiptActivity>();
        public override bool Select(IActivity activity)
        {
            if (activity is ReadReceiptActivity)
            {
                return true;
            }

            return false;
        }
    }
}

public static partial class AppEventActivityExtensions
{
    public static App OnReadReceipt(this App app, Func<IContext<ReadReceiptActivity>, Task<object?>> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = context => handler(context.ToActivityType<ReadReceiptActivity>()),
            Selector = activity =>
            {
                if (activity is ReadReceiptActivity)
                {
                    return true;
                }

                return false;
            }
        });

        return app;
    }
}