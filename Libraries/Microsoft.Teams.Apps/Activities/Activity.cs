using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class ActivityAttribute(string? name = null, Type? type = null) : Attribute
{
    public readonly ActivityType? Name = name is not null ? new(name) : null;
    public readonly Type Type = type ?? typeof(Activity);

    public virtual bool Select(IActivity activity) => Name is null || Name.Equals(activity.Type);
    public virtual object Coerce(IContext<IActivity> context) => context.ToActivityType<Activity>();
}

public static partial class AppActivityExtensions
{
    public static App OnActivity(this App app, Func<IContext<IActivity>, Task> handler)
    {
        app.Router.Register(async (context) =>
        {
            await handler(context);
            return null;
        });

        return app;
    }

    public static App OnActivity(this App app, Func<IContext<IActivity>, Task<object?>> handler)
    {
        app.Router.Register(handler);
        return app;
    }

    public static App OnActivity(this App app, ActivityType type, Func<IContext<IActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = async (context) =>
            {
                await handler(context);
                return null;
            },
            Selector = (activity) => activity.Type.Equals(type),
        });

        return app;
    }

    public static App OnActivity(this App app, ActivityType type, Func<IContext<IActivity>, Task<object?>> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = handler,
            Selector = (activity) => activity.Type.Equals(type),
        });

        return app;
    }

    public static App OnActivity<TActivity>(this App app, Func<IContext<TActivity>, Task> handler) where TActivity : IActivity
    {
        app.Router.Register(new Route()
        {
            Handler = async (context) =>
            {
                await handler(context.ToActivityType<TActivity>());
                return null;
            },
            Selector = (activity) => activity.GetType() == typeof(TActivity),
        });

        return app;
    }

    public static App OnActivity<TActivity>(this App app, Func<IContext<TActivity>, Task<object?>> handler) where TActivity : IActivity
    {
        app.Router.Register(new Route()
        {
            Handler = (context) => handler(context.ToActivityType<TActivity>()),
            Selector = (activity) => activity.GetType() == typeof(TActivity),
        });

        return app;
    }

    public static App OnActivity(this App app, Func<IActivity, bool> select, Func<IContext<IActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Selector = select,
            Handler = async (context) =>
            {
                await handler(context);
                return null;
            }
        });

        return app;
    }

    public static App OnActivity(this App app, Func<IActivity, bool> select, Func<IContext<IActivity>, Task<object?>> handler)
    {
        app.Router.Register(new Route()
        {
            Selector = select,
            Handler = handler
        });

        return app;
    }
}