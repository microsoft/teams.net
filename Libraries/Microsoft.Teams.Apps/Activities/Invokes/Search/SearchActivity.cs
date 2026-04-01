using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Api.Search;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class SearchAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.Search, typeof(SearchActivity))
{
    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<SearchActivity>();
}

public static partial class AppInvokeActivityExtensions
{
    public static App OnSearch(this App app, Func<IContext<SearchActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.Search]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<SearchActivity>());
                return null;
            },
            Selector = activity => activity is SearchActivity
        });

        return app;
    }

    public static App OnSearch(this App app, Func<IContext<SearchActivity>, Task<Response<SearchResponse>>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.Search]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context => await handler(context.ToActivityType<SearchActivity>()),
            Selector = activity => activity is SearchActivity
        });

        return app;
    }

    public static App OnSearch(this App app, Func<IContext<SearchActivity>, Task<SearchResponse>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.Search]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context => await handler(context.ToActivityType<SearchActivity>()),
            Selector = activity => activity is SearchActivity
        });

        return app;
    }

    public static App OnSearch(this App app, Func<IContext<SearchActivity>, CancellationToken, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.Search]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context =>
            {
                await handler(context.ToActivityType<SearchActivity>(), context.CancellationToken);
                return null;
            },
            Selector = activity => activity is SearchActivity
        });

        return app;
    }

    public static App OnSearch(this App app, Func<IContext<SearchActivity>, CancellationToken, Task<Response<SearchResponse>>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.Search]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context => await handler(context.ToActivityType<SearchActivity>(), context.CancellationToken),
            Selector = activity => activity is SearchActivity
        });

        return app;
    }

    public static App OnSearch(this App app, Func<IContext<SearchActivity>, CancellationToken, Task<SearchResponse>> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Invoke, Name.Search]),
            Type = app.Status is null ? RouteType.System : RouteType.User,
            Handler = async context => await handler(context.ToActivityType<SearchActivity>(), context.CancellationToken),
            Selector = activity => activity is SearchActivity
        });

        return app;
    }
}