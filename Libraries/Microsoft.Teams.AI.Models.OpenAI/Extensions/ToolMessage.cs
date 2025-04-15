using Microsoft.Teams.AI.Messages;

using OpenAI.Chat;

namespace Microsoft.Teams.AI.Models.OpenAI;

public static partial class MessageExtensions
{
    public static FunctionMessage ToTeams(this ToolChatMessage message)
    {
        return new FunctionMessage()
        {
            FunctionId = message.ToolCallId,
            Content = message.Content.FirstOrDefault()?.Text
        };
    }

    public static ToolChatMessage ToOpenAI(this FunctionMessage message)
    {
        return ChatMessage.CreateToolMessage(
            message.FunctionId,
            message.Content ?? string.Empty
        );
    }
}