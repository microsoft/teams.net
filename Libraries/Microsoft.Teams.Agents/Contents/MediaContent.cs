using Microsoft.Teams.Common.Json;

namespace Microsoft.Teams.Agents;

public static partial class Contents
{
    public static MediaContent Media(Stream stream) => new(stream);
}

public partial class ContentType
{
    public static readonly ContentType Media = new("media");
    public bool IsMedia => Media.Equals(Value);
}

[TrueTypeJson<IMediaContent>]
public interface IMediaContent : IContent
{
    public Stream Stream { get; }
}

public class MediaContent(Stream stream) : IMediaContent
{
    public ContentType Type => ContentType.Media;
    public Stream Stream { get; set; } = stream;
}