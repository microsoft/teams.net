
using Microsoft.Agents.Builder;
using Microsoft.Agents.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Plugins.Agents.Models;

namespace Microsoft.Teams.Plugins.Agents;

public class TeamsAgentMiddleware(ILogger logger, TeamsAgentPlugin plugin) : IMiddleware
{
    public async Task OnTurnAsync(Microsoft.Agents.Builder.ITurnContext ctx, NextDelegate next, CancellationToken cancellationToken = default)
    {
        ServiceCollection services = new();
        var routingStrategy = plugin.Options.RoutingStrategy;
        logger.LogDebug("{}", ctx.Activity.Type);

        if (plugin.Options.RoutingFactory is not null)
        {
            routingStrategy = await plugin.Options.RoutingFactory(ctx, cancellationToken);
        }

        if (routingStrategy == RoutingStrategy.Broadcast || routingStrategy == RoutingStrategy.Teams)
        {
            services.AddSingleton(ctx);
            plugin.Options.ContextAccessor.Value = ctx;

            foreach (var (key, service) in ctx.Services)
            {
                services.AddKeyedSingleton(service.GetType(), key, service);
            }

            var res = await plugin.Do(new()
            {
                Token = new JsonWebToken(ctx.Identity),
                Activity = ctx.Activity.ToTeamsEntity(),
                Services = services.BuildServiceProvider(),
            }, cancellationToken);

            if (ctx.Activity.IsType("invoke") && res.Body is not null)
            {
                await ctx.SendActivityAsync(Activity.CreateInvokeResponseActivity(
                    res.Body,
                    (int)res.Status
                ), cancellationToken);
            }
        }

        if (routingStrategy == RoutingStrategy.Broadcast || routingStrategy == RoutingStrategy.Agents)
        {
            await next(cancellationToken);
        }
    }
}