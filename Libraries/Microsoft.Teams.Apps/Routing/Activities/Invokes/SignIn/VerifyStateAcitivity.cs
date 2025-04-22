using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;

namespace Microsoft.Teams.Apps.Routing;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class VerifyStateAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.SignIn.VerifyState, typeof(SignIn.VerifyStateActivity))
{
    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<SignIn.VerifyStateActivity>();
}

public partial interface IRoutingModule
{
    public IRoutingModule OnVerifyState(Func<IContext<SignIn.VerifyStateActivity>, Task<object?>> handler);
}

public partial class RoutingModule : IRoutingModule
{
    public IRoutingModule OnVerifyState(Func<IContext<SignIn.VerifyStateActivity>, Task<object?>> handler)
    {
        Router.Register(new Route()
        {
            Handler = context => handler(context.ToActivityType<SignIn.VerifyStateActivity>()),
            Selector = activity =>
            {
                if (activity is SignIn.VerifyStateActivity tokenExchange)
                {
                    return true;
                }

                return false;
            }
        });

        return this;
    }
}