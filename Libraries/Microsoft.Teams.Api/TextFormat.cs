// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
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

    /// <summary>
    /// Extended markdown text format. Supports GFM tables, LaTeX math blocks,
    /// and other rich content beyond standard markdown.
    /// </summary>
    /// <remarks>
    /// This format is currently in public preview and may be subject to change.
    /// </remarks>
    [Experimental("ExperimentalTeamsExtendedMarkdown")]
    public static readonly TextFormat ExtendedMarkdown = new("extendedmarkdown");
    public bool IsExtendedMarkdown => "extendedmarkdown".Equals(Value);
}