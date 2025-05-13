using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.AI.Prompts;
using Microsoft.Teams.Common.Logging;

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
        collection.AddSingleton<IChatPrompt>(provider => provider.GetRequiredService<OpenAIChatPrompt>());
        return collection;
    }

    public static IServiceCollection AddOpenAI(this IServiceCollection collection, string model, string apiKey, ChatPromptOptions? options = null)
    {
        var chatModel = new OpenAIChatModel(model, apiKey);
        var prompt = new OpenAIChatPrompt(chatModel, options);

        collection.AddSingleton(chatModel);
        collection.AddSingleton<IChatModel<ChatCompletionOptions>, OpenAIChatModel>(provider => provider.GetRequiredService<OpenAIChatModel>());
        collection.AddSingleton(prompt);
        collection.AddSingleton<IChatPrompt>(provider => provider.GetRequiredService<OpenAIChatPrompt>());
        return collection;
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
        collection.AddSingleton<IChatPrompt>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger>();
            var model = provider.GetRequiredService<OpenAIChatModel>();
            return new OpenAIChatPrompt(model, (options ?? new()).WithLogger(logger));
        });

        return collection;
    }

    public static IServiceCollection AddOpenAI<T>(this IServiceCollection collection, ChatPromptOptions? options = null) where T : class
    {
        collection.AddSingleton<T>();
        collection.AddSingleton(provider =>
        {
            var logger = provider.GetRequiredService<ILogger>();
            var settings = provider.GetRequiredService<OpenAISettings>();
            return new OpenAIChatModel(settings.Model, settings.ApiKey, new() { Logger = logger });
        });

        collection.AddSingleton<IChatModel<ChatCompletionOptions>, OpenAIChatModel>(provider => provider.GetRequiredService<OpenAIChatModel>());
        collection.AddSingleton<IChatPrompt>(provider =>
        {
            var value = provider.GetRequiredService<T>();
            var logger = provider.GetRequiredService<ILogger>();
            var model = provider.GetRequiredService<OpenAIChatModel>();
            return OpenAIChatPrompt.From(model, value, (options ?? new()).WithLogger(logger));
        });

        return collection;
    }
}