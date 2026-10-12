// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.RegularExpressions;

namespace Microsoft.Teams.Apps.Files;

/// <summary>
/// The image formats the SDK gives an extension and a specific MIME type.
/// <para>PNG, JPEG, and WebP are named and typed with confidence.
/// GIF is not: Teams saves a pasted GIF as a static PNG, so it is listed as <c>.gif</c> and <c>image/gif</c> but downloads as a PNG.
/// Other formats, such as BMP and AVIF, are not given an extension or a specific MIME type.</para>
/// </summary>
internal enum InlineImageFormat
{
    Png,
    Jpeg,
    Gif,
    Webp,
}

/// <summary>
/// Format helpers for images pasted inline into a message.
/// </summary>
internal static class InlineImage
{
    internal const string OctetStream = "application/octet-stream";

    private const string AmsImageItemType = "http://schema.skype.com/AMSImage";

    private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Matches the path of an inline image hosted by the Bot API, capturing its attachment id.
    /// <para>Pattern: the URL's path ends in <c>/v3/attachments/&lt;id&gt;/views/original</c>, so the same text in a query string or fragment does not match.
    /// The id becomes part of the file name, so it is limited to letters, digits, <c>_</c>, <c>.</c>, and <c>-</c>, which keeps path separators out of it.</para>
    /// </summary>
    private static readonly Regex InlineImagePath = new(
        @"/v3/attachments/([A-Za-z0-9_.-]+)/views/original$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        MatchTimeout);

    // Each <img ...> start tag, skipping a `>` inside a quoted value.
    // Each character of the tag can be consumed by only one alternative, so a tag that never closes fails without retrying other splits of its text.
    private static readonly Regex ImgTag = new(
        @"<img\b(?:[^<>""']|""[^""]*""|'[^']*')*>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        MatchTimeout);

    // Each name="value", capturing the name and whichever quoted value matched.
    // A name starts only where the previous character is not part of a name, so a long run of name characters is scanned once rather than once per position.
    private static readonly Regex Attribute = new(
        @"(?<![A-Za-z0-9_-])([A-Za-z0-9_-]+)\s*=\s*(?:""([^""]*)""|'([^']*)')",
        RegexOptions.CultureInvariant,
        MatchTimeout);

    // "image/" then the subtype (letters, digits, ".", "+" or "-"), capturing the subtype.
    private static readonly Regex ImageContentType = new(
        @"^image/([A-Za-z0-9_.+-]+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        MatchTimeout);

    /// <summary>
    /// The attachment id of an inline image, taken from its <c>contentUrl</c>, or <c>null</c> when the URL is not an inline image location.
    /// </summary>
    internal static string? InlineImageId(Uri? contentUrl)
    {
        if (contentUrl is null || !contentUrl.IsAbsoluteUri)
        {
            return null;
        }

        Match match = InlineImagePath.Match(contentUrl.AbsolutePath);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Map each inline image id found in a message's HTML to its format hint.
    /// <para>The hint is the <c>itemscope</c> attribute of an <c>&lt;img&gt;</c> whose <c>itemtype</c> marks it as an inline image, keyed by its <c>itemid</c>.</para>
    /// </summary>
    internal static Dictionary<string, string> ParseInlineImageHints(string html)
    {
        Dictionary<string, string> hints = new(StringComparer.Ordinal);

        foreach (Match tag in ImgTag.Matches(html))
        {
            Dictionary<string, string> attributes = new(StringComparer.OrdinalIgnoreCase);

            foreach (Match match in Attribute.Matches(tag.Value))
            {
                attributes[match.Groups[1].Value] = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[3].Value;
            }

            attributes.TryGetValue("itemid", out string? id);
            attributes.TryGetValue("itemscope", out string? scope);
            attributes.TryGetValue("itemtype", out string? itemType);

            if (itemType == AmsImageItemType && !string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(scope))
            {
                hints[id] = scope;
            }
        }

        return hints;
    }

    /// <summary>The format named by an <c>itemscope</c> hint, or <c>null</c> when it is not one of <see cref="InlineImageFormat"/>.</summary>
    internal static InlineImageFormat? FormatFromHint(string? hint)
    {
        string? name = hint?.Trim();

        if (string.Equals(name, "png", StringComparison.OrdinalIgnoreCase))
        {
            return InlineImageFormat.Png;
        }

        if (string.Equals(name, "jpeg", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "jpg", StringComparison.OrdinalIgnoreCase))
        {
            return InlineImageFormat.Jpeg;
        }

        if (string.Equals(name, "gif", StringComparison.OrdinalIgnoreCase))
        {
            return InlineImageFormat.Gif;
        }

        if (string.Equals(name, "webp", StringComparison.OrdinalIgnoreCase))
        {
            return InlineImageFormat.Webp;
        }

        return null;
    }

    /// <summary>The format named by an <c>image/*</c> content type, or <c>null</c> when it is not one of <see cref="InlineImageFormat"/>.</summary>
    internal static InlineImageFormat? FormatFromContentType(string? contentType)
    {
        Match match = ImageContentType.Match(contentType?.Trim() ?? string.Empty);
        return match.Success ? FormatFromHint(match.Groups[1].Value) : null;
    }

    /// <summary>
    /// The format the bytes actually are, read from their leading magic bytes, or <c>null</c> when they are none of <see cref="InlineImageFormat"/>.
    /// <para>The platform can return bytes in a different format from the hint (a GIF downloads as a static PNG), so the bytes are the stronger signal.</para>
    /// </summary>
    internal static InlineImageFormat? SniffImageFormat(ReadOnlySpan<byte> bytes)
    {
        if (bytes.StartsWith(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }))
        {
            return InlineImageFormat.Png;
        }

        if (bytes.StartsWith(new byte[] { 0xff, 0xd8, 0xff }))
        {
            return InlineImageFormat.Jpeg;
        }

        if (bytes.StartsWith(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }) || bytes.StartsWith(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }))
        {
            return InlineImageFormat.Gif;
        }

        // "RIFF" at byte 0 and "WEBP" at byte 8; bytes 4-7 are the file size.
        if (bytes.StartsWith(new byte[] { 0x52, 0x49, 0x46, 0x46 }) && bytes.Length >= 12 && bytes[8..].StartsWith(new byte[] { 0x57, 0x45, 0x42, 0x50 }))
        {
            return InlineImageFormat.Webp;
        }

        return null;
    }

    /// <summary>
    /// The filename, extension, and MIME type for an inline image of the given format.
    /// <para>An unknown format gets no extension and <c>application/octet-stream</c> rather than a guess.</para>
    /// </summary>
    internal static (string Name, string? Extension, string ContentType) InlineImageNaming(string baseName, InlineImageFormat? format)
    {
#pragma warning disable CS8524
        (string extension, string contentType)? named = format switch
        {
            InlineImageFormat.Png => ("png", "image/png"),
            InlineImageFormat.Jpeg => ("jpg", "image/jpeg"),
            InlineImageFormat.Gif => ("gif", "image/gif"),
            InlineImageFormat.Webp => ("webp", "image/webp"),
            null => null,
        };
#pragma warning restore CS8524

        return named is { } n ? ($"{baseName}.{n.extension}", n.extension, n.contentType) : (baseName, null, OctetStream);
    }
}
