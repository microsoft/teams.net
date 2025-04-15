using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api;

[JsonConverter(typeof(JsonConverter<TextFormat>))]
public class TextFormat(string value) : StringEnum(value)
{
    public static readonly TextFormat Markdown = new("markdown");
    public bool IsMarkdown => Markdown.Equals(Value);

    public static readonly TextFormat Plain = new("plain");
    public bool IsPlain => Plain.Equals(Value);

    public static readonly TextFormat Xml = new("xml");
    public bool IsXml => Xml.Equals(Value);
}