using Microsoft.Teams.Api.Activities;

namespace Microsoft.Teams.Apps.Routing;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public partial class ActivityAttribute(
    string? name = null,
    Type? type = null
) : Attribute
{
    public readonly ActivityType? Name = name is not null ? new(name) : null;
    public readonly Type Type = type ?? typeof(Activity);

    public virtual bool Select(IActivity activity) => Name is null || Name.Equals(activity.Type);
    public virtual object Coerce(IContext<IActivity> context) => context.ToActivityType<Activity>();
}

public partial interface IRoutingModule
{
    public IRoutingModule OnActivity(Func<IContext<IActivity>, Task> handler);
    public IRoutingModule OnActivity(Func<IContext<IActivity>, Task<object?>> handler);
    public IRoutingModule OnActivity(ActivityType type, Func<IContext<IActivity>, Task> handler);
    public IRoutingModule OnActivity(ActivityType type, Func<IContext<IActivity>, Task<object?>> handler);
    public IRoutingModule OnActivity<TActivity>(Func<IContext<TActivity>, Task> handler) where TActivity : IActivity;
    public IRoutingModule OnActivity<TActivity>(Func<IContext<TActivity>, Task<object?>> handler) where TActivity : IActivity;
    public IRoutingModule OnActivity(Func<IActivity, bool> select, Func<IContext<IActivity>, Task> handler);
    public IRoutingModule OnActivity(Func<IActivity, bool> select, Func<IContext<IActivity>, Task<object?>> handler);
}

public partial class RoutingModule : IRoutingModule
{
    protected IRouter Router { get; } = new Router();

    public IRoutingModule OnActivity(Func<IContext<IActivity>, Task> handler)
    {
        Router.Register(async (context) =>
        {
            await handler(context);
            return null;
        });

        return this;
    }

    public IRoutingModule OnActivity(Func<IContext<IActivity>, Task<object?>> handler)
    {
        Router.Register(handler);
        return this;
    }

    public IRoutingModule OnActivity(ActivityType type, Func<IContext<IActivity>, Task> handler)
    {
        Router.Register(new Route()
        {
            Handler = async (context) =>
            {
                await handler(context);
                return null;
            },
            Selector = (activity) => activity.Type.Equals(type),
        });

        return this;
    }

    public IRoutingModule OnActivity(ActivityType type, Func<IContext<IActivity>, Task<object?>> handler)
    {
        Router.Register(new Route()
        {
            Handler = handler,
            Selector = (activity) => activity.Type.Equals(type),
        });

        return this;
    }

    public IRoutingModule OnActivity<TActivity>(Func<IContext<TActivity>, Task> handler) where TActivity : IActivity
    {
        Router.Register(new Route()
        {
            Handler = async (context) =>
            {
                await handler(context.ToActivityType<TActivity>());
                return null;
            },
            Selector = (activity) => activity.GetType() == typeof(TActivity),
        });

        return this;
    }

    public IRoutingModule OnActivity<TActivity>(Func<IContext<TActivity>, Task<object?>> handler) where TActivity : IActivity
    {
        Router.Register(new Route()
        {
            Handler = (context) => handler(context.ToActivityType<TActivity>()),
            Selector = (activity) => activity.GetType() == typeof(TActivity),
        });

        return this;
    }

    public IRoutingModule OnActivity(Func<IActivity, bool> select, Func<IContext<IActivity>, Task> handler)
    {
        Router.Register(new Route()
        {
            Selector = select,
            Handler = async (context) =>
            {
                await handler(context);
                return null;
            }
        });

        return this;
    }

    public IRoutingModule OnActivity(Func<IActivity, bool> select, Func<IContext<IActivity>, Task<object?>> handler)
    {
        Router.Register(new Route()
        {
            Selector = select,
            Handler = handler
        });

        return this;
    }
}