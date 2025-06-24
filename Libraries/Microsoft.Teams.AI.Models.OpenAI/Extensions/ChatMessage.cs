// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.AI.Messages;

using OpenAI.Chat;

namespace Microsoft.Teams.AI.Models.OpenAI;

public static partial class MessageExtensions
{
    public static IMessage ToTeams(this ChatMessage message)
    {
        if (message is SystemChatMessage system) return system.ToTeams();
        if (message is AssistantChatMessage assistant) return assistant.ToTeams();
        if (message is ToolChatMessage tool) return tool.ToTeams();
        if (message is UserChatMessage user) return user.ToTeams();
        throw new Exception("OpenAI ChatMessage type not supported");
    }

    public static ChatMessage ToOpenAI(this IMessage message)
    {
        if (message is DeveloperMessage developer) return developer.ToOpenAI();
        if (message is ModelMessage<string> model) return model.ToOpenAI();
        if (message is FunctionMessage function) return function.ToOpenAI();
        if (message is UserMessage<string> userText) return userText.ToOpenAI();
        if (message is UserMessage<IEnumerable<IContent>> userParts) return userParts.ToOpenAI();
        throw new Exception("Teams Message type not supported");
    }
}