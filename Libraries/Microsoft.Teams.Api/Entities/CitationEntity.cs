// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Entities;

public class CitationEntity : OMessageEntity, IMessageEntity
{
    [JsonPropertyName("citation")]
    [JsonPropertyOrder(20)]
    public IList<Claim>? Citation { get; set; }

    public CitationEntity() : base()
    {
    }

    public CitationEntity(IMessageEntity entity)
        : base()
    {
        OType = entity.OType;
        OContext = entity.OContext;
        Type = entity.Type;
        AdditionalType = entity.AdditionalType != null
            ? new List<string>(entity.AdditionalType)
            : null;
        if (entity is CitationEntity citationEntity)
        {
            Citation = citationEntity.Citation != null
                ? new List<CitationEntity.Claim>(citationEntity.Citation)
                : null;
        }
    }

    public class Claim
    {
        [JsonPropertyName("@type")]
        [JsonPropertyOrder(0)]
        public string Type { get; set; } = "Claim";

        [JsonPropertyName("position")]
        [JsonPropertyOrder(1)]
        public required int Position { get; set; }

        [JsonPropertyName("appearance")]
        [JsonPropertyOrder(2)]
        public required AppearanceDocument Appearance { get; set; }
    }

    public class AppearanceDocument
    {
        [JsonPropertyName("@type")]
        [JsonPropertyOrder(0)]
        public string Type { get; set; } = "DigitalDocument";

        [JsonPropertyName("name")]
        [JsonPropertyOrder(1)]
        public required string Name { get; set; }

        [JsonPropertyName("text")]
        [JsonPropertyOrder(2)]
        public string? Text { get; set; }

        [JsonPropertyName("url")]
        [JsonPropertyOrder(3)]
        public string? Url { get; set; }

        [JsonPropertyName("abstract")]
        [JsonPropertyOrder(4)]
        public required string Abstract { get; set; }

        [JsonPropertyName("encodingFormat")]
        [JsonPropertyOrder(5)]
        public ContentType? EncodingFormat { get; set; }

        [JsonPropertyName("image")]
        [JsonPropertyOrder(6)]
        public ImageObject? Image { get; set; }

        [JsonPropertyName("keywords")]
        [JsonPropertyOrder(7)]
        public List<string>? Keywords { get; set; }

        [JsonPropertyName("usageInfo")]
        [JsonPropertyOrder(8)]
        public SensitiveUsageEntity? UsageInfo { get; set; }

        public class ImageObject
        {
            [JsonPropertyName("@type")]
            [JsonPropertyOrder(0)]
            public string Type { get; set; } = "ImageObject";

            [JsonPropertyName("name")]
            [JsonPropertyOrder(1)]
            public required CitationIcon Name { get; set; }
        }
    }
}

[JsonConverter(typeof(JsonConverter<CitationIcon>))]
public class CitationIcon(string value) : StringEnum(value)
{
    public static readonly CitationIcon MicrosoftWord = new("Microsoft Word");
    public bool IsMicrosoftWord => MicrosoftWord.Equals(Value);

    public static readonly CitationIcon MicrosoftExcel = new("Microsoft Excel");
    public bool IsMicrosoftExcel => MicrosoftExcel.Equals(Value);

    public static readonly CitationIcon MicrosoftPowerPoint = new("Microsoft PowerPoint");
    public bool IsMicrosoftPowerPoint => MicrosoftPowerPoint.Equals(Value);

    public static readonly CitationIcon MicrosoftOneNote = new("Microsoft OneNote");
    public bool IsMicrosoftOneNote => MicrosoftOneNote.Equals(Value);

    public static readonly CitationIcon MicrosoftSharePoint = new("Microsoft SharePoint");
    public bool IsMicrosoftSharePoint => MicrosoftSharePoint.Equals(Value);

    public static readonly CitationIcon MicrosoftVisio = new("Microsoft Visio");
    public bool IsMicrosoftVisio => MicrosoftVisio.Equals(Value);

    public static readonly CitationIcon MicrosoftLoop = new("Microsoft Loop");
    public bool IsMicrosoftLoop => MicrosoftLoop.Equals(Value);

    public static readonly CitationIcon MicrosoftWhiteboard = new("Microsoft Whiteboard");
    public bool IsMicrosoftWhiteboard => MicrosoftWhiteboard.Equals(Value);

    public static readonly CitationIcon AdobeIllustrator = new("Adobe Illustrator");
    public bool IsAdobeIllustrator => AdobeIllustrator.Equals(Value);

    public static readonly CitationIcon AdobePhotoshop = new("Adobe Photoshop");
    public bool IsAdobePhotoshop => AdobePhotoshop.Equals(Value);

    public static readonly CitationIcon AdobeInDesign = new("Adobe InDesign");
    public bool IsAdobeInDesign => AdobeInDesign.Equals(Value);

    public static readonly CitationIcon AdobeFlash = new("Adobe Flash");
    public bool IsAdobeFlash => AdobeFlash.Equals(Value);

    public static readonly CitationIcon Sketch = new("Sketch");
    public bool IsSketch => Sketch.Equals(Value);

    public static readonly CitationIcon SourceCode = new("Source Code");
    public bool IsSourceCode => SourceCode.Equals(Value);

    public static readonly CitationIcon Image = new("Image");
    public bool IsImage => Image.Equals(Value);

    public static readonly CitationIcon Gif = new("GIF");
    public bool IsGif => Gif.Equals(Value);

    public static readonly CitationIcon Video = new("Video");
    public bool IsVideo => Video.Equals(Value);

    public static readonly CitationIcon Sound = new("Sound");
    public bool IsSound => Sound.Equals(Value);

    public static readonly CitationIcon Zip = new("ZIP");
    public bool IsZip => Zip.Equals(Value);

    public static readonly CitationIcon Text = new("Text");
    public bool IsText => Text.Equals(Value);

    public static readonly CitationIcon Pdf = new("PDF");
    public bool IsPdf => Pdf.Equals(Value);
}

public class CitationAppearance
{
    public required string Name { get; set; }
    public string? Text { get; set; }
    public string? Url { get; set; }
    public required string Abstract { get; set; }
    public ContentType? EncodingFormat { get; set; }
    public CitationIcon? Icon { get; set; }
    public List<string>? Keywords { get; set; }
    public SensitiveUsageEntity? UsageInfo { get; set; }

    public CitationEntity.AppearanceDocument ToDocument()
    {
        return new()
        {
            Name = Name,
            Text = Text,
            Url = Url,
            Abstract = Abstract,
            EncodingFormat = EncodingFormat,
            Image = Icon is null ? null : new CitationEntity.AppearanceDocument.ImageObject() { Name = Icon },
            Keywords = Keywords,
            UsageInfo = UsageInfo
        };
    }
}