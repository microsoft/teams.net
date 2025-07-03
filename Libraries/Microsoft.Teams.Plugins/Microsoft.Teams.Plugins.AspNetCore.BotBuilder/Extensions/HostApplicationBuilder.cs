// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Teams.Plugins.AspNetCore.Controllers;

namespace Microsoft.Teams.Plugins.AspNetCore.Extensions;

public static class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddBotBuilder(this IHostApplicationBuilder builder)
    {
        builder.Services.AddControllers().ConfigureApplicationPartManager((apm) =>
        {
            apm.FeatureProviders.Add(new RemoveDefaultMessageController());
            apm.ApplicationParts.Add(new AssemblyPart(typeof(MessageController).Assembly));
        });
        return builder;
    }

    public static IHostApplicationBuilder AddBotBuilder<TBot>(this IHostApplicationBuilder builder, BotFrameworkAuthentication authentication, IBotFrameworkHttpAdapter adapter) where TBot : class, IBot
    {
        builder.Services.AddSingleton(authentication);
        builder.Services.AddSingleton(adapter);
        builder.Services.AddTransient<IBot, TBot>();
        builder.Services.AddControllers().ConfigureApplicationPartManager((apm) =>
        {
            apm.FeatureProviders.Add(new RemoveDefaultMessageController());
            apm.ApplicationParts.Add(new AssemblyPart(typeof(MessageController).Assembly));
        });
        return builder;
    }

    public static IHostApplicationBuilder AddBotBuilder<TBot, TBotFrameworkHttpAdapter, TBotFrameworkAuthentication>(this IHostApplicationBuilder builder) where TBot : class, IBot where TBotFrameworkAuthentication : BotFrameworkAuthentication where TBotFrameworkHttpAdapter : class, IBotFrameworkHttpAdapter
    {
        builder.Services.AddSingleton<BotFrameworkAuthentication, TBotFrameworkAuthentication>();
        builder.Services.AddSingleton<IBotFrameworkHttpAdapter, TBotFrameworkHttpAdapter>();
        builder.Services.AddTransient<IBot, TBot>();
        builder.Services.AddControllers().ConfigureApplicationPartManager((apm) =>
        {
            apm.FeatureProviders.Add(new RemoveDefaultMessageController());
            apm.ApplicationParts.Add(new AssemblyPart(typeof(MessageController).Assembly));
        });
        return builder;
    }
}