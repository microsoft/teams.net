using Microsoft.Teams.AI.Messages;

using OpenAI.Chat;

namespace Microsoft.Teams.AI.Models.OpenAI;

public static partial class MessageExtensions
{
    public static UserMessage<IEnumerable<IContent>> ToTeams(this UserChatMessage message)
    {
        var parts = message.Content.Select<ChatMessageContentPart, IContent>(part =>
        {
            if (part.Kind == ChatMessageContentPartKind.Text)
            {
                return new TextContent() { Text = part.Text };
            }

            return new ImageContent() { ImageUrl = part.ImageUri.ToString() };
        });

        return new(parts);
    }

    public static UserChatMessage ToOpenAI(this UserMessage<IEnumerable<IContent>> message)
    {
        var parts = message.Content.Select(part =>
        {
            if (part is TextContent text)
            {
                return ChatMessageContentPart.CreateTextPart(text.Text);
            }

            if (part is ImageContent image)
            {
                return ChatMessageContentPart.CreateImagePart(new Uri(image.ImageUrl));
            }

            throw new Exception("invalid content part");
        });

        return ChatMessage.CreateUserMessage(parts);
    }

    public static UserChatMessage ToOpenAI(this UserMessage<string> message)
    {
        return ChatMessage.CreateUserMessage(message.Content);
    }
}