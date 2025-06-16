using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class CommandAttribute() : ActivityAttribute(ActivityType.Command, type: typeof(CommandActivity))
{
    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<CommandActivity>();
}

public static partial class AppActivityExtensions
{
    public static App OnCommand(this App app, Func<IContext<CommandActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = async context =>
            {
                await handler(context.ToActivityType<CommandActivity>());
                return null;
            },
            Selector = activity => activity is CommandActivity
        });

        return app;
    }
}