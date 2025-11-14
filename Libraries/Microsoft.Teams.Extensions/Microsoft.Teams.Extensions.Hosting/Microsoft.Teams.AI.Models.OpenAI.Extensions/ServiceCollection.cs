// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.AI.Prompts;

using OpenAI.Chat;

namespace Microsoft.Teams.AI.Models.OpenAI.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenAI(this IServiceCollection collection, OpenAIChatModel model, ChatPromptOptions? options = null)
    {
        collection.AddSingleton(model);
        collection.AddSingleton<IChatModel<ChatCompletionOptions>, OpenAIChatModel>(provider => provider.GetRequiredService<OpenAIChatModel>());
        collection.AddSingleton(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<OpenAIChatPrompt>>();
            return new OpenAIChatPrompt(model, options, logger);
        });
        return collection.AddSingleton<IChatPrompt>(provider => provider.GetRequiredService<OpenAIChatPrompt>());
    }

    public static IServiceCollection AddOpenAI(this IServiceCollection collection, string model, string apiKey, ChatPromptOptions? options = null)
    {
        collection.AddSingleton(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<OpenAIChatModel>>();
            return new OpenAIChatModel(model, apiKey, logger);
        });
        collection.AddSingleton<IChatModel<ChatCompletionOptions>, OpenAIChatModel>(provider => provider.GetRequiredService<OpenAIChatModel>());
        collection.AddSingleton(provider =>
        {
            var modelInstance = provider.GetRequiredService<OpenAIChatModel>();
            var logger = provider.GetRequiredService<ILogger<OpenAIChatPrompt>>();
            return new OpenAIChatPrompt(modelInstance, options, logger);
        });
        return collection.AddSingleton<IChatPrompt>(provider => provider.GetRequiredService<OpenAIChatPrompt>());
    }

    public static IServiceCollection AddOpenAI(this IServiceCollection collection, ChatPromptOptions? options = null)
    {
        collection.AddSingleton(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<OpenAIChatModel>>();
            var settings = provider.GetRequiredService<OpenAISettings>();
            return new OpenAIChatModel(settings.Model, settings.ApiKey, logger);
        });

        collection.AddSingleton<IChatModel<ChatCompletionOptions>, OpenAIChatModel>(provider => provider.GetRequiredService<OpenAIChatModel>());
        collection.AddSingleton(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<OpenAIChatPrompt>>();
            var model = provider.GetRequiredService<OpenAIChatModel>();
            return new OpenAIChatPrompt(model, options, logger);
        });

        return collection.AddSingleton<IChatPrompt>(provider => provider.GetRequiredService<OpenAIChatPrompt>());
    }

    public static IServiceCollection AddOpenAI<T>(this IServiceCollection collection, ChatPromptOptions? options = null) where T : class
    {
        collection.AddScoped(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<OpenAIChatModel>>();
            var settings = provider.GetRequiredService<OpenAISettings>();
            return new OpenAIChatModel(settings.Model, settings.ApiKey, logger);
        });

        return collection.AddOpenAIHelper<T>(options);

    }

    public static IServiceCollection AddOpenAI<T>(this IServiceCollection collection, OpenAIChatModel model, ChatPromptOptions? options = null) where T : class
    {
        collection.AddScoped(provider => model);
        return collection.AddOpenAIHelper<T>(options);
    }

    private static IServiceCollection AddOpenAIHelper<T>(this IServiceCollection collection, ChatPromptOptions? options) where T : class
    {
        collection.AddScoped<T>();
        collection.AddScoped<IChatModel<ChatCompletionOptions>, OpenAIChatModel>(
            provider => provider.GetRequiredService<OpenAIChatModel>()
        );

        collection.AddScoped(provider =>
        {
            var value = provider.GetRequiredService<T>();
            var logger = provider.GetRequiredService<ILogger<OpenAIChatPrompt>>();
            var model = provider.GetRequiredService<OpenAIChatModel>();
            return OpenAIChatPrompt.From(model, value, options, logger);
        });

        collection.AddScoped<IChatPrompt>(
            provider => provider.GetRequiredService<OpenAIChatPrompt>()
        );

        // Add a factory for creating scoped prompts by accessing the HttpContext
        collection.AddSingleton<Func<OpenAIChatPrompt>>(provider =>
        {
            return () =>
            {
                var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
                var httpContext = httpContextAccessor.HttpContext
                    ?? throw new InvalidOperationException("No active HttpContext. Cannot resolve OpenAIChatPrompt.");

                return httpContext.RequestServices.GetRequiredService<OpenAIChatPrompt>();
            };
        });

        return collection;
    }
}