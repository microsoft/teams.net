using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

public static partial class MessageExtension
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class QueryAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.MessageExtensions.Query, typeof(MessageExtensions.QueryActivity))
    {
        public override object Coerce(IContext<IActivity> context) => context.ToActivityType<MessageExtensions.QueryActivity>();
    }
}

public static partial class AppInvokeActivityExtensions
{
    public static App OnQuery(this App app, Func<IContext<MessageExtensions.QueryActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = async context =>
            {
                await handler(context.ToActivityType<MessageExtensions.QueryActivity>());
                return null;
            },
            Selector = activity => activity is MessageExtensions.QueryActivity
        });

        return app;
    }

    public static App OnQuery(this App app, Func<IContext<MessageExtensions.QueryActivity>, Task<Response<Api.MessageExtensions.Response>>> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = async context => await handler(context.ToActivityType<MessageExtensions.QueryActivity>()),
            Selector = activity => activity is MessageExtensions.QueryActivity
        });

        return app;
    }

    public static App OnQuery(this App app, Func<IContext<MessageExtensions.QueryActivity>, Task<Api.MessageExtensions.Response>> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = async context => await handler(context.ToActivityType<MessageExtensions.QueryActivity>()),
            Selector = activity => activity is MessageExtensions.QueryActivity
        });

        return app;
    }
}