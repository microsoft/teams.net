// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.Teams.Apps.Utils;

namespace Microsoft.Teams.Apps.Schema.Entities;

/// <summary>
/// Citation entity representing a message with citation claims.
/// </summary>
public class CitationEntity : OMessageEntity
{
    /// <summary>
    /// Creates a new instance of <see cref="CitationEntity"/>.
    /// </summary>
    public CitationEntity() : base()
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="CitationEntity"/> by copying data from an existing message entity.
    /// </summary>
    /// <param name="entity">The message entity to copy from. Cannot be null.</param>
    public CitationEntity(OMessageEntity entity) : base()
    {
        ArgumentNullException.ThrowIfNull(entity);
        OType = entity.OType;
        OContext = entity.OContext;
        Type = entity.Type;
        Properties = new Core.Schema.ExtendedPropertiesDictionary(entity.Properties);
        AdditionalType = entity.AdditionalType != null
            ? new List<string>(entity.AdditionalType)
            : null;
        if (entity is CitationEntity citationEntity)
        {
            Citation = citationEntity.Citation != null
                ? citationEntity.Citation.Select(c => new CitationClaim(c)).ToList()
                : null;
        }
    }

    /// <summary>
    /// Gets or sets the list of citation claims.
    /// </summary>
    [JsonPropertyName("citation")]
    public IList<CitationClaim>? Citation
    {
        get => base.Properties.Get<IList<CitationClaim>>("citation");
        set => base.Properties["citation"] = value;
    }
}

/// <summary>
/// Represents a citation claim with a position and appearance document.
/// </summary>
public class CitationClaim
{
    /// <summary>Initializes a new instance of <see cref="CitationClaim"/>.</summary>
    public CitationClaim() { }

    /// <summary>Creates a deep copy of <paramref name="other"/>.</summary>
    [SetsRequiredMembers]
    public CitationClaim(CitationClaim other)
    {
        ArgumentNullException.ThrowIfNull(other);
        Type = other.Type;
        Position = other.Position;
        Appearance = new CitationAppearanceDocument(other.Appearance);
    }

    /// <summary>
    /// Gets or sets the schema.org type. Always "Claim".
    /// </summary>
    [JsonPropertyName("@type")]
    public string Type { get; set; } = "Claim";

    /// <summary>
    /// Gets or sets the position of the citation in the message text.
    /// </summary>
    [JsonPropertyName("position")]
    public required int Position { get; set; }

    /// <summary>
    /// Gets or sets the appearance document describing the cited source.
    /// </summary>
    [JsonPropertyName("appearance")]
    public required CitationAppearanceDocument Appearance { get; set; }
}

/// <summary>
/// Represents the appearance of a cited document.
/// </summary>
public class CitationAppearanceDocument
{
    /// <summary>Initializes a new instance of <see cref="CitationAppearanceDocument"/>.</summary>
    public CitationAppearanceDocument() { }

    /// <summary>Creates a deep copy of <paramref name="other"/>.</summary>
    [SetsRequiredMembers]
    public CitationAppearanceDocument(CitationAppearanceDocument other)
    {
        ArgumentNullException.ThrowIfNull(other);
        Type = other.Type;
        Name = other.Name;
        Text = other.Text;
        Url = other.Url;
        Abstract = other.Abstract;
        EncodingFormat = other.EncodingFormat;
        Image = other.Image is null ? null : new CitationImageObject { Type = other.Image.Type, Name = other.Image.Name };
        Keywords = other.Keywords != null ? new List<string>(other.Keywords) : null;
        UsageInfo = other.UsageInfo;
    }

    /// <summary>
    /// Gets or sets the schema.org type. Always "DigitalDocument".
    /// </summary>
    [JsonPropertyName("@type")]
    public string Type { get; set; } = "DigitalDocument";

    /// <summary>
    /// Gets or sets the name of the document (max length 80).
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets a stringified adaptive card with additional information about the citation.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the URL of the document.
    /// </summary>
    [JsonPropertyName("url")]
    public Uri? Url { get; set; }

    /// <summary>
    /// Gets or sets the extract of the referenced content (max length 160).
    /// </summary>
    [JsonPropertyName("abstract")]
    public required string Abstract { get; set; }

    /// <summary>
    /// Gets or sets the encoding format of the text. See <see cref="EncodingFormats"/> for known values.
    /// </summary>
    [JsonPropertyName("encodingFormat")]
    public EncodingFormat? EncodingFormat { get; set; }

    /// <summary>
    /// Gets or sets the citation icon information.
    /// </summary>
    [JsonPropertyName("image")]
    public CitationImageObject? Image { get; set; }

    /// <summary>
    /// Gets or sets the keywords (max length 3, max keyword length 28).
    /// </summary>
    [JsonPropertyName("keywords")]
    public IList<string>? Keywords { get; set; }

    /// <summary>
    /// Gets or sets the sensitivity usage information for the citation.
    /// </summary>
    [JsonPropertyName("usageInfo")]
    public SensitiveUsageEntity? UsageInfo { get; set; }
}

/// <summary>
/// Represents an image object used for citation icons.
/// </summary>
public class CitationImageObject
{
    /// <summary>
    /// Gets or sets the schema.org type. Always "ImageObject".
    /// </summary>
    [JsonPropertyName("@type")]
    public string Type { get; set; } = "ImageObject";

    /// <summary>
    /// Gets or sets the icon name. See <see cref="CitationIcons"/> for known values.
    /// </summary>
    [JsonPropertyName("name")]
    public required CitationIcon Name { get; set; }
}

/// <summary>
/// Known citation icon names.
/// </summary>
[JsonConverter(typeof(StringEnumJsonConverter<CitationIcon>))]
public class CitationIcon(string value) : StringEnum(value)
{
    /// <summary>Microsoft Word icon.</summary>
    public static readonly CitationIcon MicrosoftWord = new("Microsoft Word");
    /// <summary>Microsoft Excel icon.</summary>
    public static readonly CitationIcon MicrosoftExcel = new("Microsoft Excel");
    /// <summary>Microsoft PowerPoint icon.</summary>
    public static readonly CitationIcon MicrosoftPowerPoint = new("Microsoft PowerPoint");
    /// <summary>Microsoft OneNote icon.</summary>
    public static readonly CitationIcon MicrosoftOneNote = new("Microsoft OneNote");
    /// <summary>Microsoft SharePoint icon.</summary>
    public static readonly CitationIcon MicrosoftSharePoint = new("Microsoft SharePoint");
    /// <summary>Microsoft Visio icon.</summary>
    public static readonly CitationIcon MicrosoftVisio = new("Microsoft Visio");
    /// <summary>Microsoft Loop icon.</summary>
    public static readonly CitationIcon MicrosoftLoop = new("Microsoft Loop");
    /// <summary>Microsoft Whiteboard icon.</summary>
    public static readonly CitationIcon MicrosoftWhiteboard = new("Microsoft Whiteboard");
    /// <summary>Adobe Illustrator icon.</summary>
    public static readonly CitationIcon AdobeIllustrator = new("Adobe Illustrator");
    /// <summary>Adobe Photoshop icon.</summary>
    public static readonly CitationIcon AdobePhotoshop = new("Adobe Photoshop");
    /// <summary>Adobe InDesign icon.</summary>
    public static readonly CitationIcon AdobeInDesign = new("Adobe InDesign");
    /// <summary>Adobe Flash icon.</summary>
    public static readonly CitationIcon AdobeFlash = new("Adobe Flash");
    /// <summary>Sketch icon.</summary>
    public static readonly CitationIcon Sketch = new("Sketch");
    /// <summary>Source code icon.</summary>
    public static readonly CitationIcon SourceCode = new("Source Code");
    /// <summary>Image icon.</summary>
    public static readonly CitationIcon Image = new("Image");
    /// <summary>GIF icon.</summary>
    public static readonly CitationIcon Gif = new("GIF");
    /// <summary>Video icon.</summary>
    public static readonly CitationIcon Video = new("Video");
    /// <summary>Sound icon.</summary>
    public static readonly CitationIcon Sound = new("Sound");
    /// <summary>ZIP icon.</summary>
    public static readonly CitationIcon Zip = new("ZIP");
    /// <summary>Text icon.</summary>
    public static readonly CitationIcon Text = new("Text");
    /// <summary>PDF icon.</summary>
    public static readonly CitationIcon Pdf = new("PDF");
}

/// <summary>
/// Known citation icon names.
/// </summary>
public static class CitationIcons
{
    /// <summary>Microsoft Word icon.</summary>
    public static CitationIcon MicrosoftWord => CitationIcon.MicrosoftWord;
    /// <summary>Microsoft Excel icon.</summary>
    public static CitationIcon MicrosoftExcel => CitationIcon.MicrosoftExcel;
    /// <summary>Microsoft PowerPoint icon.</summary>
    public static CitationIcon MicrosoftPowerPoint => CitationIcon.MicrosoftPowerPoint;
    /// <summary>Microsoft OneNote icon.</summary>
    public static CitationIcon MicrosoftOneNote => CitationIcon.MicrosoftOneNote;
    /// <summary>Microsoft SharePoint icon.</summary>
    public static CitationIcon MicrosoftSharePoint => CitationIcon.MicrosoftSharePoint;
    /// <summary>Microsoft Visio icon.</summary>
    public static CitationIcon MicrosoftVisio => CitationIcon.MicrosoftVisio;
    /// <summary>Microsoft Loop icon.</summary>
    public static CitationIcon MicrosoftLoop => CitationIcon.MicrosoftLoop;
    /// <summary>Microsoft Whiteboard icon.</summary>
    public static CitationIcon MicrosoftWhiteboard => CitationIcon.MicrosoftWhiteboard;
    /// <summary>Adobe Illustrator icon.</summary>
    public static CitationIcon AdobeIllustrator => CitationIcon.AdobeIllustrator;
    /// <summary>Adobe Photoshop icon.</summary>
    public static CitationIcon AdobePhotoshop => CitationIcon.AdobePhotoshop;
    /// <summary>Adobe InDesign icon.</summary>
    public static CitationIcon AdobeInDesign => CitationIcon.AdobeInDesign;
    /// <summary>Adobe Flash icon.</summary>
    public static CitationIcon AdobeFlash => CitationIcon.AdobeFlash;
    /// <summary>Sketch icon.</summary>
    public static CitationIcon Sketch => CitationIcon.Sketch;
    /// <summary>Source code icon.</summary>
    public static CitationIcon SourceCode => CitationIcon.SourceCode;
    /// <summary>Image icon.</summary>
    public static CitationIcon Image => CitationIcon.Image;
    /// <summary>GIF icon.</summary>
    public static CitationIcon Gif => CitationIcon.Gif;
    /// <summary>Video icon.</summary>
    public static CitationIcon Video => CitationIcon.Video;
    /// <summary>Sound icon.</summary>
    public static CitationIcon Sound => CitationIcon.Sound;
    /// <summary>ZIP icon.</summary>
    public static CitationIcon Zip => CitationIcon.Zip;
    /// <summary>Text icon.</summary>
    public static CitationIcon Text => CitationIcon.Text;
    /// <summary>PDF icon.</summary>
    public static CitationIcon Pdf => CitationIcon.Pdf;
}

/// <summary>
/// Known encoding format MIME types for citation documents.
/// </summary>
[JsonConverter(typeof(StringEnumJsonConverter<EncodingFormat>))]
public class EncodingFormat(string value) : StringEnum(value)
{
    /// <summary>Adaptive card encoding format.</summary>
    public static readonly EncodingFormat AdaptiveCard = new("application/vnd.microsoft.card.adaptive");
    /// <summary>text/plain encoding format.</summary>
    public static readonly EncodingFormat TextPlain = new("text/plain");
}

/// <summary>
/// Known encoding format MIME types for citation documents.
/// </summary>
public static class EncodingFormats
{
    /// <summary>Adaptive card encoding format.</summary>
    public static EncodingFormat AdaptiveCard => EncodingFormat.AdaptiveCard;

    /// <summary>text/plain encoding format.</summary>
    public static EncodingFormat TextPlain => EncodingFormat.TextPlain;
}

/// <summary>
/// Helper class for building citation appearance documents.
/// </summary>
public class CitationAppearance
{
    /// <summary>
    /// Gets or sets the name of the document (max length 80).
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets a stringified adaptive card with additional information.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the URL of the document.
    /// </summary>
    public Uri? Url { get; set; }

    /// <summary>
    /// Gets or sets the extract of the referenced content (max length 160).
    /// </summary>
    public required string Abstract { get; set; }

    /// <summary>
    /// Gets or sets the encoding format of the text. See <see cref="EncodingFormats"/> for known values.
    /// </summary>
    public EncodingFormat? EncodingFormat { get; set; }

    /// <summary>
    /// Gets or sets the citation icon name. See <see cref="CitationIcons"/> for known values.
    /// </summary>
    public CitationIcon? Icon { get; set; }

    /// <summary>
    /// Gets or sets the keywords (max length 3, max keyword length 28).
    /// </summary>
    public IList<string>? Keywords { get; set; }

    /// <summary>
    /// Gets or sets the sensitivity usage information.
    /// </summary>
    public SensitiveUsageEntity? UsageInfo { get; set; }

    /// <summary>
    /// Converts this appearance to a <see cref="CitationAppearanceDocument"/>.
    /// </summary>
    /// <returns>The appearance document.</returns>
    public CitationAppearanceDocument ToDocument()
    {
        return new()
        {
            Name = Name,
            Text = Text,
            Url = Url,
            Abstract = Abstract,
            EncodingFormat = EncodingFormat,
            Image = Icon is null ? null : new CitationImageObject() { Name = Icon },
            Keywords = Keywords,
            UsageInfo = UsageInfo
        };
    }
}
