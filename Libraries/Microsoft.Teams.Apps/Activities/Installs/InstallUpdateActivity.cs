using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class InstallUpdateAttribute() : ActivityAttribute(ActivityType.InstallUpdate, typeof(InstallUpdateActivity))
{
    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<InstallUpdateActivity>();
}

public static partial class AppActivityExtensions
{
    public static App OnInstallUpdate(this App app, Func<IContext<InstallUpdateActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = async context =>
            {
                await handler(context.ToActivityType<InstallUpdateActivity>());
                return null;
            },
            Selector = activity => activity is InstallUpdateActivity
        });

        return app;
    }
}