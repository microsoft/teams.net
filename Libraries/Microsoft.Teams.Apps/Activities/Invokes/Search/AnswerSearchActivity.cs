using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Api.Search;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

public static partial class Search
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class Answer() : SearchAttribute
    {
        public override bool Select(IActivity activity)
        {
            if (activity is SearchActivity search)
            {
                return search.Value.Kind.IsSearchAnswer;
            }

            return false;
        }
    }
}

public static partial class AppInvokeActivityExtensions
{
    public static App OnAnswerSearch(this App app, Func<IContext<SearchActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = async context =>
            {
                await handler(context.ToActivityType<SearchActivity>());
                return null;
            },
            Selector = activity =>
            {
                if (activity is SearchActivity search)
                {
                    return search.Value.Kind.IsSearchAnswer;
                }

                return false;
            }
        });

        return app;
    }

    public static App OnAnswerSearch(this App app, Func<IContext<SearchActivity>, Task<Response<SearchResponse>>> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = async context => await handler(context.ToActivityType<SearchActivity>()),
            Selector = activity =>
            {
                if (activity is SearchActivity search)
                {
                    return search.Value.Kind.IsSearchAnswer;
                }

                return false;
            }
        });

        return app;
    }

    public static App OnAnswerSearch(this App app, Func<IContext<SearchActivity>, Task<SearchResponse>> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = async context => await handler(context.ToActivityType<SearchActivity>()),
            Selector = activity =>
            {
                if (activity is SearchActivity search)
                {
                    return search.Value.Kind.IsSearchAnswer;
                }

                return false;
            }
        });

        return app;
    }
}