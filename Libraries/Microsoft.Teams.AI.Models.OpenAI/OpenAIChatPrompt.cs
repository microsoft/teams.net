// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.AI.Prompts;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace Microsoft.Teams.AI.Models.OpenAI;

public class OpenAIChatPrompt : ChatPrompt<ChatCompletionOptions>
{
    public OpenAIChatPrompt(OpenAIChatModel model, ChatPromptOptions? options = null, ILogger<OpenAIChatPrompt>? logger = null) : base(model, options, logger)
    {

    }

    public OpenAIChatPrompt(ChatPrompt<ChatCompletionOptions> prompt) : base(prompt)
    {

    }

    public OpenAIChatPrompt(string name, ChatPrompt<ChatCompletionOptions> prompt) : base(name, prompt)
    {

    }

    public static OpenAIChatPrompt From<T>(OpenAIChatModel model, T value, ChatPromptOptions? options = null, ILogger<OpenAIChatPrompt>? logger = null) where T : class
    {
        var prompt = ChatPrompt<ChatCompletionOptions>.From(model, value, options, logger);
        return new OpenAIChatPrompt(prompt);
    }
}