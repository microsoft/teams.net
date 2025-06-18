using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

public static partial class MessageExtension
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class QueryLinkAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.MessageExtensions.QueryLink, typeof(MessageExtensions.QueryLinkActivity))
    {
        public override object Coerce(IContext<IActivity> context) => context.ToActivityType<MessageExtensions.QueryLinkActivity>();
    }
}

public static partial class AppInvokeActivityExtensions
{
    public static App OnQueryLink(this App app, Func<IContext<MessageExtensions.QueryLinkActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = async context =>
            {
                await handler(context.ToActivityType<MessageExtensions.QueryLinkActivity>());
                return null;
            },
            Selector = activity => activity is MessageExtensions.QueryLinkActivity
        });

        return app;
    }

    public static App OnQueryLink(this App app, Func<IContext<MessageExtensions.QueryLinkActivity>, Task<Response<Api.MessageExtensions.Response>>> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = async context => await handler(context.ToActivityType<MessageExtensions.QueryLinkActivity>()),
            Selector = activity => activity is MessageExtensions.QueryLinkActivity
        });

        return app;
    }

    public static App OnQueryLink(this App app, Func<IContext<MessageExtensions.QueryLinkActivity>, Task<Api.MessageExtensions.Response>> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = async context => await handler(context.ToActivityType<MessageExtensions.QueryLinkActivity>()),
            Selector = activity => activity is MessageExtensions.QueryLinkActivity
        });

        return app;
    }
}