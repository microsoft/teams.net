using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class TokenExchangeAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.SignIn.TokenExchange, typeof(SignIn.TokenExchangeActivity))
{
    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<SignIn.TokenExchangeActivity>();
}

public static partial class AppExtensions
{
    public static App OnTokenExchange(this App app, Func<IContext<SignIn.TokenExchangeActivity>, Task<object?>> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = context => handler(context.ToActivityType<SignIn.TokenExchangeActivity>()),
            Selector = activity =>
            {
                if (activity is SignIn.TokenExchangeActivity tokenExchange)
                {
                    return true;
                }

                return false;
            }
        });

        return app;
    }
}