using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class VerifyStateAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.SignIn.VerifyState, typeof(SignIn.VerifyStateActivity))
{
    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<SignIn.VerifyStateActivity>();
}

public static partial class AppInvokeActivityExtensions
{
    public static App OnVerifyState(this App app, Func<IContext<SignIn.VerifyStateActivity>, Task<object?>> handler)
    {
        app.Router.Register(new Route()
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

        return app;
    }
}