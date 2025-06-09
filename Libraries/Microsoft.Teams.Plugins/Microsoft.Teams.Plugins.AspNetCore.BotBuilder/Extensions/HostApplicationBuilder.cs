using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Teams.Plugins.AspNetCore.Controllers;

namespace Microsoft.Teams.Plugins.AspNetCore.Extensions;

public static class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddBotBuilder(this IHostApplicationBuilder builder)
    {
        builder.Services.AddControllers().ConfigureApplicationPartManager((apm) => {
            apm.FeatureProviders.Add(new RemoveDefaultMessageController());
            apm.ApplicationParts.Add(new AssemblyPart(typeof(MessageController).Assembly));
        });
        return builder;
    }
}