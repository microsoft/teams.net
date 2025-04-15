using Microsoft.Teams.Api.Activities;

namespace Microsoft.Teams.Apps.Routing;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class InstallUpdateAttribute() : ActivityAttribute(ActivityType.InstallUpdate, typeof(InstallUpdateActivity))
{
    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<InstallUpdateActivity>();
}

public partial interface IRoutingModule
{
    public IRoutingModule OnInstallUpdate(Func<IContext<InstallUpdateActivity>, Task> handler);
}

public partial class RoutingModule : IRoutingModule
{
    public IRoutingModule OnInstallUpdate(Func<IContext<InstallUpdateActivity>, Task> handler)
    {
        Router.Register(new Route()
        {
            Handler = async context =>
            {
                await handler(context.ToActivityType<InstallUpdateActivity>());
                return null;
            },
            Selector = activity =>
            {
                if (activity is InstallUpdateActivity installUpdate)
                {
                    return true;
                }

                return false;
            }
        });

        return this;
    }
}