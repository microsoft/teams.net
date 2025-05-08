using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.Api.Activities;

namespace Microsoft.Teams.Apps.Extensions;

public static class ServiceProviderExtensions
{
    public static IContext<IActivity> GetTeamsContext(this IServiceProvider provider)
    {
        return provider.GetRequiredService<IContext<IActivity>>();
    }

    public static IActivity GetTeamsActivity(this IServiceProvider provider)
    {
        return provider.GetRequiredService<IContext<IActivity>>().Activity;
    }
}