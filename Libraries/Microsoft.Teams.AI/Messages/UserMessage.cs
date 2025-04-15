using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.AI.Messages;

public class UserMessage(object? content) : UserMessage<object?>(content)
{
    public static UserMessage<string> Text(string content) => new(content);
    public static UserMessage<IEnumerable<IContent>> Text(IEnumerable<IContent> content) => new(content);
    public static UserMessage<Stream> Media(Stream content) => new(content);
}

public class UserMessage<T> : IMessage
{
    [JsonPropertyName("role")]
    [JsonPropertyOrder(0)]
    public Role Role => Role.User;

    [JsonPropertyName("content")]
    [JsonPropertyOrder(1)]
    public T Content { get; set; }

    [JsonConstructor]
    public UserMessage(T content)
    {
        Content = content;
    }

    public string GetText()
    {
        if (Content is IEnumerable<IContent> asEnum)
        {
            return string.Join("\n", asEnum.Select(v => v.ToString()));
        }

        if (Content is string asString)
        {
            return asString;
        }

        return Content?.ToString() ?? throw new InvalidCastException();
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }
}

[JsonConverter(typeof(JsonConverter<ContentType>))]
public class ContentType(string value) : StringEnum(value)
{
    public static readonly ContentType Text = new("text");
    public bool IsText => Text.Equals(Value);

    public static readonly ContentType ImageUrl = new("image_url");
    public bool IsImageUrl => ImageUrl.Equals(Value);
}

/// <summary>
/// represents some message content
/// </summary>
public interface IContent
{
    /// <summary>
    /// the type of content
    /// </summary>
    public ContentType Type { get; }
}

public class TextContent : IContent
{
    [JsonPropertyName("type")]
    [JsonPropertyOrder(0)]
    public ContentType Type => ContentType.Text;

    [JsonPropertyName("text")]
    [JsonPropertyOrder(1)]
    public required string Text { get; set; }

    public override string ToString() => Text;
}

public class ImageContent : IContent
{
    [JsonPropertyName("type")]
    [JsonPropertyOrder(0)]
    public ContentType Type => ContentType.ImageUrl;

    [JsonPropertyName("image_url")]
    [JsonPropertyOrder(1)]
    public required string ImageUrl { get; set; }

    public override string ToString() => ImageUrl;
}