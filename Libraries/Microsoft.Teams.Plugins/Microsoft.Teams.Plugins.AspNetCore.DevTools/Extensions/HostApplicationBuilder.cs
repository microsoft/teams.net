﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Teams.Apps.Extensions;

namespace Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;

public static class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddTeamsDevTools(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton(builder.Configuration.GetTeamsDevTools());
        builder.Services.AddTeamsPlugin<DevToolsPlugin>();
        builder.Services.AddControllers().AddApplicationPart(Assembly.GetExecutingAssembly());
        return builder;
    }
}