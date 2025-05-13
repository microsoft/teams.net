using Microsoft.Teams.AI.Prompts;

using OpenAI.Chat;

namespace Microsoft.Teams.AI.Models.OpenAI;

public class OpenAIChatPrompt : ChatPrompt<ChatCompletionOptions>
{
    public OpenAIChatPrompt(OpenAIChatModel model, ChatPromptOptions? options = null) : base(model, options)
    {

    }

    public OpenAIChatPrompt(ChatPrompt<ChatCompletionOptions> prompt) : base(prompt)
    {

    }

    public OpenAIChatPrompt(string name, ChatPrompt<ChatCompletionOptions> prompt) : base(name, prompt)
    {

    }

    public static OpenAIChatPrompt From<T>(OpenAIChatModel model, T value, ChatPromptOptions? options = null) where T : class
    {
        var prompt = ChatPrompt<ChatCompletionOptions>.From(model, value, options);
        return new OpenAIChatPrompt(prompt);
    }
}