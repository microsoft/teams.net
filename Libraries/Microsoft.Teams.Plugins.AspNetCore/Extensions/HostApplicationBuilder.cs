using System.Reflection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Extensions;

namespace Microsoft.Teams.Plugins.AspNetCore.Extensions;

public static class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddTeams(this IHostApplicationBuilder builder)
    {
        builder.AddTeamsCore();
        builder.AddTeamsPlugin<AspNetCorePlugin>();
        builder.Services.AddControllers().AddApplicationPart(Assembly.GetExecutingAssembly());
        return builder;
    }

    public static IHostApplicationBuilder AddTeams(this IHostApplicationBuilder builder, IApp app)
    {
        builder.AddTeamsCore(app);
        builder.AddTeamsPlugin<AspNetCorePlugin>();
        builder.Services.AddControllers().AddApplicationPart(Assembly.GetExecutingAssembly());
        return builder;
    }

    public static IHostApplicationBuilder AddTeams(this IHostApplicationBuilder builder, IAppOptions options)
    {
        builder.AddTeamsCore(options);
        builder.AddTeamsPlugin<AspNetCorePlugin>();
        builder.Services.AddControllers().AddApplicationPart(Assembly.GetExecutingAssembly());
        return builder;
    }

    public static IHostApplicationBuilder AddTeams(this IHostApplicationBuilder builder, IAppBuilder appBuilder)
    {
        builder.AddTeamsCore(appBuilder);
        builder.AddTeamsPlugin<AspNetCorePlugin>();
        builder.Services.AddControllers().AddApplicationPart(Assembly.GetExecutingAssembly());
        return builder;
    }
}