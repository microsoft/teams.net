using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;

namespace Microsoft.Teams.Plugins.Agents.Extensions;

public static class ContextExtensions
{
    public static Microsoft.Agents.Builder.ITurnContext GetTurnContext<TActivity>(this IContext<TActivity> context) where TActivity : IActivity
    {
        if (!context.Extra.TryGetValue("agents.context", out var value))
        {
            throw new InvalidOperationException("ITurnContext not found");
        }

        if (value is not Microsoft.Agents.Builder.ITurnContext turnContext)
        {
            throw new InvalidOperationException("invalid ITurnContext type");
        }

        return turnContext;
    }

    public static Microsoft.Agents.Builder.ITurnContext<TActivity> GetTurnContext<TActivity>(this IContext<IActivity> context) where TActivity : Microsoft.Agents.Core.Models.IActivity
    {
        if (!context.Extra.TryGetValue("agents.context", out var value))
        {
            throw new InvalidOperationException("ITurnContext not found");
        }

        if (value is not Microsoft.Agents.Builder.ITurnContext<TActivity> turnContext)
        {
            throw new InvalidOperationException("invalid ITurnContext type");
        }

        return turnContext;
    }

    public static Microsoft.Agents.Builder.State.ITurnState GetTurnState<TActivity>(this IContext<TActivity> context) where TActivity : IActivity
    {
        if (!context.Extra.TryGetValue("agents.state", out var value))
        {
            throw new InvalidOperationException("ITurnState not found");
        }

        if (value is not Microsoft.Agents.Builder.State.ITurnState state)
        {
            throw new InvalidOperationException("invalid ITurnState type");
        }

        return state;
    }
}