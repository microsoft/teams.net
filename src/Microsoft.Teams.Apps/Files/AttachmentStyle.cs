// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Teams.Apps.Utils;

namespace Microsoft.Teams.Apps.Files;

/// <summary>
/// How a file was attached to the inbound message.
/// <list type="bullet">
/// <item><description><see cref="File"/>: sent as a file attachment</description></item>
/// <item><description><see cref="InlineImage"/>: pasted into the message body</description></item>
/// </list>
/// <para>Some image formats, such as BMP and AVIF, cannot be converted by the Teams service, so Teams shows a placeholder, and they cannot be downloaded.
/// The same image sent as a file attachment downloads normally.</para>
/// </summary>
[JsonConverter(typeof(StringEnumJsonConverter<AttachmentStyle>))]
public class AttachmentStyle(string value) : StringEnum(value)
{
    /// <summary>Sent as a file attachment.</summary>
    public static readonly AttachmentStyle File = new("file");
    /// <summary>Pasted into the message body.</summary>
    public static readonly AttachmentStyle InlineImage = new("inlineImage");
}

/// <summary>
/// Common attachment style values.
/// </summary>
public static class AttachmentStyles
{
    /// <summary>Gets the file attachment style.</summary>
    public static AttachmentStyle File => AttachmentStyle.File;

    /// <summary>Gets the inline image attachment style.</summary>
    public static AttachmentStyle InlineImage => AttachmentStyle.InlineImage;
}
