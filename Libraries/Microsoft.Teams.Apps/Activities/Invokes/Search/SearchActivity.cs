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
            Handler = async context => await handler(context.ToActivityType<SearchActivity>()),
            Selector = activity => activity is SearchActivity
        });

        return app;
    }

    public static App OnSearch(this App app, Func<IContext<SearchActivity>, Task<SearchResponse>> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = async context => await handler(context.ToActivityType<SearchActivity>()),
            Selector = activity => activity is SearchActivity
        });

        return app;
    }
}