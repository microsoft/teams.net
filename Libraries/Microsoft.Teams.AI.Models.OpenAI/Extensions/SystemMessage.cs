using Microsoft.Teams.AI.Messages;

using OpenAI.Chat;

namespace Microsoft.Teams.AI.Models.OpenAI;

public static partial class MessageExtensions
{
    public static DeveloperMessage ToTeams(this SystemChatMessage message)
    {
        var content = message.Content.Select(c =>
        {
            if (c.Kind == ChatMessageContentPartKind.Text) return c.Text;
            if (c.Kind == ChatMessageContentPartKind.Image) return c.ImageUri.ToString();
            return c.Refusal;
        });

        return new DeveloperMessage(string.Join("\n", content));
    }

    public static SystemChatMessage ToOpenAI(this DeveloperMessage message)
    {
        return ChatMessage.CreateSystemMessage(message.Content);
    }
}