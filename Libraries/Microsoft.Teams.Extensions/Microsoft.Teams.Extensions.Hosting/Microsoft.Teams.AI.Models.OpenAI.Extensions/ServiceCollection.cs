// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.AI.Prompts;
using Microsoft.Teams.Common.Logging;
using Microsoft.AspNetCore.Http;

using OpenAI.Chat;

namespace Microsoft.Teams.AI.Models.OpenAI.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenAI(this IServiceCollection collection, OpenAIChatModel model, ChatPromptOptions? options = null)
    {
        var prompt = new OpenAIChatPrompt(model, options);

        collection.AddSingleton(model);
        collection.AddSingleton<IChatModel<ChatCompletionOptions>, OpenAIChatModel>(provider => provider.GetRequiredService<OpenAIChatModel>());
        collection.AddSingleton(prompt);
        return collection.AddSingleton<IChatPrompt>(provider => provider.GetRequiredService<OpenAIChatPrompt>());
    }

    public static IServiceCollection AddOpenAI(this IServiceCollection collection, string model, string apiKey, ChatPromptOptions? options = null)
    {
        var chatModel = new OpenAIChatModel(model, apiKey);
        var prompt = new OpenAIChatPrompt(chatModel, options);

        collection.AddSingleton(chatModel);
        collection.AddSingleton<IChatModel<ChatCompletionOptions>, OpenAIChatModel>(provider => provider.GetRequiredService<OpenAIChatModel>());
        collection.AddSingleton(prompt);
        return collection.AddSingleton<IChatPrompt>(provider => provider.GetRequiredService<OpenAIChatPrompt>());
    }

    public static IServiceCollection AddOpenAI(this IServiceCollection collection, ChatPromptOptions? options = null)
    {
        collection.AddSingleton(provider =>
        {
            var logger = provider.GetRequiredService<ILogger>();
            var settings = provider.GetRequiredService<OpenAISettings>();
            return new OpenAIChatModel(settings.Model, settings.ApiKey, new() { Logger = logger });
        });

        collection.AddSingleton<IChatModel<ChatCompletionOptions>, OpenAIChatModel>(provider => provider.GetRequiredService<OpenAIChatModel>());
        collection.AddSingleton(provider =>
        {
            var logger = provider.GetRequiredService<ILogger>();
            var model = provider.GetRequiredService<OpenAIChatModel>();
            return new OpenAIChatPrompt(model, (options ?? new()).WithLogger(logger));
        });

        return collection.AddSingleton<IChatPrompt>(provider => provider.GetRequiredService<OpenAIChatPrompt>());
    }

    public static IServiceCollection AddOpenAI<T>(this IServiceCollection collection, ChatPromptOptions? options = null) where T : class
    {
        collection.AddScoped<T>();
        collection.AddScoped(provider =>
        {
            var logger = provider.GetRequiredService<ILogger>();
            var settings = provider.GetRequiredService<OpenAISettings>();
            return new OpenAIChatModel(settings.Model, settings.ApiKey, new() { Logger = logger });
        });

        collection.AddScoped<IChatModel<ChatCompletionOptions>, OpenAIChatModel>(provider => provider.GetRequiredService<OpenAIChatModel>());
        collection.AddScoped(provider =>
        {
            var value = provider.GetRequiredService<T>();
            var logger = provider.GetRequiredService<ILogger>();
            var model = provider.GetRequiredService<OpenAIChatModel>();
            return OpenAIChatPrompt.From(model, value, (options ?? new()).WithLogger(logger));
        });

        collection.AddScoped<IChatPrompt>(provider => provider.GetRequiredService<OpenAIChatPrompt>());
        collection.AddHttpContextAccessor();
        // Add a factory for creating scoped prompts by accessing the HttpContext
        collection.AddSingleton<Func<OpenAIChatPrompt>>(provider =>
        {
            return () =>
            {
                IHttpContextAccessor _httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
                var httpContext = _httpContextAccessor.HttpContext
                  ?? throw new InvalidOperationException("No active HttpContext. Cannot resolve OpenAIChatPrompt.");

                return httpContext.RequestServices.GetRequiredService<OpenAIChatPrompt>();
            };
        });

        return collection;
    }

    public static IServiceCollection AddOpenAI<T>(this IServiceCollection collection, OpenAIChatModel model, ChatPromptOptions? options = null) where T : class
    {
        collection.AddScoped<T>();
        collection.AddScoped(provider => model);

        collection.AddScoped<IChatModel<ChatCompletionOptions>, OpenAIChatModel>(provider => provider.GetRequiredService<OpenAIChatModel>());
        collection.AddScoped(provider =>
        {
            var value = provider.GetRequiredService<T>();
            var logger = provider.GetRequiredService<ILogger>();
            var model = provider.GetRequiredService<OpenAIChatModel>();
            return OpenAIChatPrompt.From(model, value, (options ?? new()).WithLogger(logger));
        });

        collection.AddScoped<IChatPrompt>(provider => provider.GetRequiredService<OpenAIChatPrompt>());
        collection.AddHttpContextAccessor();
        // Add a factory for creating scoped prompts by accessing the HttpContext
        collection.AddSingleton<Func<OpenAIChatPrompt>>(provider =>
        {
            return () =>
            {
                IHttpContextAccessor _httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
                var httpContext = _httpContextAccessor.HttpContext
                  ?? throw new InvalidOperationException("No active HttpContext. Cannot resolve OpenAIChatPrompt.");

                return httpContext.RequestServices.GetRequiredService<OpenAIChatPrompt>();
            };
        });

        return collection;
    }
}