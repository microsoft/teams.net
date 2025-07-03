// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.AI.Messages;

using OpenAI.Chat;

namespace Microsoft.Teams.AI.Models.OpenAI;

public static partial class MessageExtensions
{
    public static ModelMessage<string> ToTeams(this AssistantChatMessage message)
    {
        var calls = message.ToolCalls.Select(call =>
        {
            var args = call.FunctionArguments.ToString();
            return new FunctionCall()
            {
                Id = call.Id,
                Name = call.FunctionName,
                Arguments = args == string.Empty ? null : args
            };
        });

        return new ModelMessage<string>(message.Content.FirstOrDefault()?.Text ?? string.Empty, calls.ToList());
    }

    public static AssistantChatMessage ToOpenAI(this ModelMessage<string> message)
    {
        var calls = message.FunctionCalls?.Select(call => ChatToolCall.CreateFunctionToolCall(
            call.Id,
            call.Name,
            call.Arguments is null ? BinaryData.Empty : BinaryData.FromString(call.Arguments)
        ));

        if (calls is not null && calls.Count() > 0)
        {
            return new AssistantChatMessage(calls?.ToList() ?? []);
        }

        return new AssistantChatMessage(message.Content);
    }
}