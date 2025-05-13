using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class TaskFetchAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.Tasks.Fetch, typeof(Tasks.FetchActivity))
{
    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<Tasks.FetchActivity>();
}

public static partial class AppInvokeActivityExtensions
{
    public static App OnTaskFetch(this App app, Func<IContext<Tasks.FetchActivity>, Task<object?>> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = context => handler(context.ToActivityType<Tasks.FetchActivity>()),
            Selector = activity => activity is Tasks.FetchActivity
        });

        return app;
    }
}