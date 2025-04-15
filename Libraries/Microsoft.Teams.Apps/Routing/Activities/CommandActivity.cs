using Microsoft.Teams.Api.Activities;

namespace Microsoft.Teams.Apps.Routing;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class CommandAttribute() : ActivityAttribute(ActivityType.Command, type: typeof(CommandActivity))
{
    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<CommandActivity>();
}

public partial interface IRoutingModule
{
    public IRoutingModule OnCommand(Func<IContext<CommandActivity>, Task<object?>> handler);
}

public partial class RoutingModule : IRoutingModule
{
    public IRoutingModule OnCommand(Func<IContext<CommandActivity>, Task<object?>> handler)
    {
        Router.Register(new Route()
        {
            Handler = context => handler(context.ToActivityType<CommandActivity>()),
            Selector = activity =>
            {
                if (activity is CommandActivity command)
                {
                    return true;
                }

                return false;
            }
        });

        return this;
    }
}