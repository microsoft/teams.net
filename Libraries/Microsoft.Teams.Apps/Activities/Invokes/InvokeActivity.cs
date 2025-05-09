using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class InvokeAttribute(string? name = null, Type? type = null) : ActivityAttribute(ActivityType.Invoke, type ?? typeof(InvokeActivity))
{
    public readonly Name? InvokeName = name is not null ? new(name) : null;

    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<InvokeActivity>();
    public override bool Select(IActivity activity)
    {
        if (activity is InvokeActivity invoke)
        {
            return invoke.Name.Equals(InvokeName);
        }

        return false;
    }
}

public static partial class AppExtensions
{
    public static App OnInvoke(this App app, Func<IContext<InvokeActivity>, Task<object?>> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = context => handler(context.ToActivityType<InvokeActivity>()),
            Selector = activity =>
            {
                if (activity is InvokeActivity invoke)
                {
                    return true;
                }

                return false;
            }
        });

        return app;
    }
}