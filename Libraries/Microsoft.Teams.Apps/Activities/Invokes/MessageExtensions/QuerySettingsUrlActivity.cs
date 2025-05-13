using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

public static partial class MessageExtension
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class QuerySettingsUrlAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.MessageExtensions.QuerySettingsUrl, typeof(MessageExtensions.QuerySettingsUrlActivity))
    {
        public override object Coerce(IContext<IActivity> context) => context.ToActivityType<MessageExtensions.QuerySettingsUrlActivity>();
    }
}

public static partial class AppInvokeActivityExtensions
{
    public static App OnQuerySettingsUrl(this App app, Func<IContext<MessageExtensions.QuerySettingsUrlActivity>, Task<object?>> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = context => handler(context.ToActivityType<MessageExtensions.QuerySettingsUrlActivity>()),
            Selector = activity =>
            {
                if (activity is MessageExtensions.QuerySettingsUrlActivity)
                {
                    return true;
                }

                return false;
            }
        });

        return app;
    }
}