// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Teams.Plugins.AspNetCore.Extensions;

public static class ServiceProviderExtensions
{
    public static AspNetCorePlugin GetAspNetCorePlugin(this IServiceProvider provider)
    {
        return provider.GetRequiredService<AspNetCorePlugin>();
    }
}