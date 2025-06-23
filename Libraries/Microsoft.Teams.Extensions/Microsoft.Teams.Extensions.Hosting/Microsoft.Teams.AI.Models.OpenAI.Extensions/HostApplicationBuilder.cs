// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Teams.AI.Models.OpenAI.Extensions;

public static class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddOpenAI(this IHostApplicationBuilder builder)
    {
        var settings = builder.Configuration.GetOpenAI();
        builder.Services.AddSingleton(settings);
        builder.Services.AddOpenAI();
        return builder;
    }

    public static IHostApplicationBuilder AddOpenAI<T>(this IHostApplicationBuilder builder) where T : class
    {
        var settings = builder.Configuration.GetOpenAI();
        builder.Services.AddSingleton(settings);
        builder.Services.AddOpenAI<T>();
        return builder;
    }
}