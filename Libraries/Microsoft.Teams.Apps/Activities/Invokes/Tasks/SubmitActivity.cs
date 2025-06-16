using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class TaskSubmitAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.Tasks.Submit, typeof(Tasks.SubmitActivity))
{
    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<Tasks.SubmitActivity>();
}

public static partial class AppInvokeActivityExtensions
{
    public static App OnTaskSubmit(this App app, Func<IContext<Tasks.SubmitActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = async context =>
            {
                await handler(context.ToActivityType<Tasks.SubmitActivity>());
                return null;
            },
            Selector = activity => activity is Tasks.SubmitActivity
        });

        return app;
    }

    public static App OnTaskSubmit(this App app, Func<IContext<Tasks.SubmitActivity>, Task<Response<Api.TaskModules.Response>>> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = async context => await handler(context.ToActivityType<Tasks.SubmitActivity>()),
            Selector = activity => activity is Tasks.SubmitActivity
        });

        return app;
    }

    public static App OnTaskSubmit(this App app, Func<IContext<Tasks.SubmitActivity>, Task<Api.TaskModules.Response>> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = async context => await handler(context.ToActivityType<Tasks.SubmitActivity>()),
            Selector = activity => activity is Tasks.SubmitActivity
        });

        return app;
    }
}