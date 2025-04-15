using Microsoft.Teams.Common.Json;

namespace Microsoft.Teams.Agents;

public static partial class Contents
{
    public static TextContent Text(string text) => new(text);
}

public partial class ContentType
{
    public static readonly ContentType Text = new("text");
    public bool IsText => Text.Equals(Value);
}

[TrueTypeJson<ITextContent>]
public interface ITextContent : IContent
{
    public string Text { get; }
}

public class TextContent(string text) : ITextContent
{
    public ContentType Type => ContentType.Text;
    public string Text { get; set; } = text;
}