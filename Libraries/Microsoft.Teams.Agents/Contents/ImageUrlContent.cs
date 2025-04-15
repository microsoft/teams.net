using Microsoft.Teams.Common.Json;

namespace Microsoft.Teams.Agents;

public static partial class Contents
{
    public static ImageUrlContent ImageUrl(string imageUrl) => new(imageUrl);
}

public partial class ContentType
{
    public static readonly ContentType ImageUrl = new("image_url");
    public bool IsImageUrl => ImageUrl.Equals(Value);
}

[TrueTypeJson<IImageUrlContent>]
public interface IImageUrlContent : IContent
{
    public string ImageUrl { get; }
}

public class ImageUrlContent(string imageUrl) : IImageUrlContent
{
    public ContentType Type => ContentType.ImageUrl;
    public string ImageUrl { get; set; } = imageUrl;
}