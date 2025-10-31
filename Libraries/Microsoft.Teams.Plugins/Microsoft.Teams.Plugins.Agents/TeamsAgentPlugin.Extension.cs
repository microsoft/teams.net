using Microsoft.Agents.Builder.State;
using Microsoft.Agents.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Plugins.Agents.Models;

namespace Microsoft.Teams.Plugins.Agents;

public partial class TeamsAgentPlugin
{
    public async Task Do(Microsoft.Agents.Builder.ITurnContext context, ITurnState state, CancellationToken cancellationToken = default)
    {
        ServiceCollection services = new();
        Logger.Debug(context.Activity.Type);
        services.AddSingleton(context);
        services.AddSingleton(state);
        Options.ContextAccessor.Value = context;

        foreach (var (key, service) in context.Services)
        {
            services.AddKeyedSingleton(service.GetType(), key, service);
        }

        var extra = new Dictionary<string, object?>
        {
            { "agents.context", context },
            { "agents.state", state }
        };

        var res = await Do(new()
        {
            Token = new JsonWebToken(context.Identity),
            Activity = context.Activity.ToTeamsEntity(),
            Services = services.BuildServiceProvider(),
            Extra = extra
        }, cancellationToken);

        if (context.Activity.IsType("invoke") && res.Body is not null)
        {
            await context.SendActivityAsync(Activity.CreateInvokeResponseActivity(
                res.Body,
                (int)res.Status
            ), cancellationToken);
        }
    }
}