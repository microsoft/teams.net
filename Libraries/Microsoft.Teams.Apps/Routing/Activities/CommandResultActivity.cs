using Microsoft.Teams.Api.Activities;

namespace Microsoft.Teams.Apps.Routing;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class CommandResultAttribute() : ActivityAttribute(ActivityType.CommandResult, typeof(CommandResultActivity))
{
    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<CommandResultActivity>();
}

public partial interface IRoutingModule
{
    public IRoutingModule OnCommandResult(Func<IContext<CommandResultActivity>, Task<object?>> handler);
}

public partial class RoutingModule : IRoutingModule
{
    public IRoutingModule OnCommandResult(Func<IContext<CommandResultActivity>, Task<object?>> handler)
    {
        Router.Register(new Route()
        {
            Handler = context => handler(context.ToActivityType<CommandResultActivity>()),
            Selector = activity =>
            {
                if (activity is CommandResultActivity commandResult)
                {
                    return true;
                }

                return false;
            }
        });

        return this;
    }
}