// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Teams.Plugins.AspNetCore.Extensions;

public static class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddBotBuilder(this IHostApplicationBuilder builder)
    {
        return builder;
    }

    public static IHostApplicationBuilder AddBotBuilder<TBot>(this IHostApplicationBuilder builder, BotFrameworkAuthentication authentication, IBotFrameworkHttpAdapter adapter) where TBot : class, IBot
    {
        builder.Services.AddSingleton(authentication);
        builder.Services.AddSingleton(adapter);
        builder.Services.AddTransient<IBot, TBot>();

        return builder;
    }

    public static IHostApplicationBuilder AddBotBuilder<TBot, TBotFrameworkHttpAdapter, TBotFrameworkAuthentication>(this IHostApplicationBuilder builder) where TBot : class, IBot where TBotFrameworkAuthentication : BotFrameworkAuthentication where TBotFrameworkHttpAdapter : class, IBotFrameworkHttpAdapter
    {
        builder.Services.AddSingleton<BotFrameworkAuthentication, TBotFrameworkAuthentication>();
        builder.Services.AddSingleton<IBotFrameworkHttpAdapter, TBotFrameworkHttpAdapter>();
        builder.Services.AddTransient<IBot, TBot>();

        return builder;
    }
}