// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Teams.Apps.Extensions;

namespace Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;

public static class HostApplicationBuilderExtensions
{
    [Obsolete("DevTools is deprecated and will be removed in a later version. Use Microsoft 365 Agents Playground instead.")]
    public static IHostApplicationBuilder AddTeamsDevTools(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton(builder.Configuration.GetTeamsDevTools());
        builder.Services.AddTeamsPlugin<DevToolsPlugin>();
        builder.Services.AddControllers().AddApplicationPart(Assembly.GetExecutingAssembly());
        return builder;
    }
}