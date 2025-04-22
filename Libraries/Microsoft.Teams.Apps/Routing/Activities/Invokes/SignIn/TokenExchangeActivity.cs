using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;

namespace Microsoft.Teams.Apps.Routing;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class TokenExchangeAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.SignIn.TokenExchange, typeof(SignIn.TokenExchangeActivity))
{
    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<SignIn.TokenExchangeActivity>();
}

public partial interface IRoutingModule
{
    public IRoutingModule OnTokenExchange(Func<IContext<SignIn.TokenExchangeActivity>, Task<object?>> handler);
}

public partial class RoutingModule : IRoutingModule
{
    public IRoutingModule OnTokenExchange(Func<IContext<SignIn.TokenExchangeActivity>, Task<object?>> handler)
    {
        Router.Register(new Route()
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

        return this;
    }
}