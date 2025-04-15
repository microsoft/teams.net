using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;

namespace Microsoft.Teams.Apps.Routing;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class InvokeAttribute(string? name = null, Type? type = null, IContext.Property log = IContext.Property.None) : ActivityAttribute(ActivityType.Invoke, type ?? typeof(InvokeActivity), log)
{
    public readonly Name? InvokeName = name != null ? new(name) : null;

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

public partial interface IRoutingModule
{
    public IRoutingModule OnInvoke(Func<IContext<InvokeActivity>, Task<object?>> handler);
}

public partial class RoutingModule : IRoutingModule
{
    public IRoutingModule OnInvoke(Func<IContext<InvokeActivity>, Task<object?>> handler)
    {
        Router.Register(new Route()
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

        return this;
    }
}