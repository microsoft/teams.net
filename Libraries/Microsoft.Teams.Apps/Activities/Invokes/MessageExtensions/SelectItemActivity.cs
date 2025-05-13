using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

public static partial class MessageExtension
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class SelectItemAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.MessageExtensions.SelectItem, typeof(MessageExtensions.SelectItemActivity))
    {
        public override object Coerce(IContext<IActivity> context) => context.ToActivityType<MessageExtensions.SelectItemActivity>();
    }
}

public static partial class AppInvokeActivityExtensions
{
    public static App OnSelectItem(this App app, Func<IContext<MessageExtensions.SelectItemActivity>, Task<object?>> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = context => handler(context.ToActivityType<MessageExtensions.SelectItemActivity>()),
            Selector = activity => activity is MessageExtensions.SelectItemActivity
        });

        return app;
    }
}