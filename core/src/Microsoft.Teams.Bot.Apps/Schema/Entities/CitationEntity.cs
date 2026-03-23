// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Bot.Apps.Schema.Entities;

/// <summary>
/// Extension methods for Activity to handle citations and AI-generated content.
/// </summary>
public static class ActivityCitationExtensions
{
    /// <summary>
    /// Adds a citation to the activity. Creates or updates the root message entity
    /// with the specified citation claim.
    /// </summary>
    /// <param name="activity">The activity to add the citation to. Cannot be null.</param>
    /// <param name="position">The position of the citation in the message text.</param>
    /// <param name="appearance">The citation appearance information.</param>
    /// <returns>The created CitationEntity that was added to the activity.</returns>
    public static CitationEntity AddCitation(this TeamsActivity activity, int position, CitationAppearance appearance)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(appearance);

        activity.Entities ??= [];
        OMessageEntity messageEntity = GetOrCreateRootMessageEntity(activity);
        CitationEntity citationEntity = new(messageEntity);
        citationEntity.Citation ??= [];
        citationEntity.Citation.Add(new CitationClaim()
        {
            Position = position,
            Appearance = appearance.ToDocument()
        });

        activity.Entities.Remove(messageEntity);
        activity.Entities.Add(citationEntity);
        activity.Rebase();
        return citationEntity;
    }

    /// <summary>
    /// Adds the AI-generated content label to the activity's root message entity.
    /// This method is idempotent — calling it multiple times has the same effect as calling it once.
    /// </summary>
    /// <param name="activity">The activity to mark as AI-generated. Cannot be null.</param>
    /// <returns>The OMessageEntity with the AI-generated label applied.</returns>
    public static OMessageEntity AddAIGenerated(this TeamsActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        OMessageEntity messageEntity = GetOrCreateRootMessageEntity(activity);
        messageEntity.AdditionalType ??= [];

        if (!messageEntity.AdditionalType.Contains("AIGeneratedContent"))
        {
            messageEntity.AdditionalType.Add("AIGeneratedContent");
        }

        activity.Rebase();
        return messageEntity;
    }

    /// <summary>
    /// Enables the feedback loop (thumbs up/down) on the activity's channel data.
    /// </summary>
    /// <param name="activity">The activity to enable feedback on. Cannot be null.</param>
    /// <param name="value">Whether to enable feedback. Defaults to true.</param>
    /// <returns>The activity for chaining.</returns>
    public static TeamsActivity AddFeedback(this TeamsActivity activity, bool value = true)
    {
        ArgumentNullException.ThrowIfNull(activity);

        activity.ChannelData ??= new TeamsChannelData();
        activity.ChannelData.FeedbackLoopEnabled = value;
        return activity;
    }

    // Gets or creates the single root-level OMessageEntity on the activity.
    private static OMessageEntity GetOrCreateRootMessageEntity(TeamsActivity activity)
    {
        activity.Entities ??= [];

        OMessageEntity? messageEntity = activity.Entities.FirstOrDefault(
            e => e.Type == "https://schema.org/Message" && e.OType == "Message"
        ) as OMessageEntity;

        if (messageEntity is null)
        {
            messageEntity = new OMessageEntity();
            activity.Entities.Add(messageEntity);
        }

        return messageEntity;
    }
}

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
        AdditionalType = entity.AdditionalType != null
            ? new List<string>(entity.AdditionalType)
            : null;
        if (entity is CitationEntity citationEntity)
        {
            Citation = citationEntity.Citation != null
                ? new List<CitationClaim>(citationEntity.Citation)
                : null;
        }
    }

    /// <summary>
    /// Gets or sets the list of citation claims.
    /// </summary>
    [JsonPropertyName("citation")]
    public IList<CitationClaim>? Citation
    {
        get => base.Properties.TryGetValue("citation", out object? value) ? value as IList<CitationClaim> : null;
        set => base.Properties["citation"] = value;
    }
}

/// <summary>
/// Represents a citation claim with a position and appearance document.
/// </summary>
public class CitationClaim
{
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
    public string? EncodingFormat { get; set; }

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
    /// Gets or sets the icon name. See <see cref="CitationIcon"/> for known values.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }
}

/// <summary>
/// Known citation icon names.
/// </summary>
public static class CitationIcon
{
    /// <summary>Microsoft Word icon.</summary>
    public const string MicrosoftWord = "Microsoft Word";

    /// <summary>Microsoft Excel icon.</summary>
    public const string MicrosoftExcel = "Microsoft Excel";

    /// <summary>Microsoft PowerPoint icon.</summary>
    public const string MicrosoftPowerPoint = "Microsoft PowerPoint";

    /// <summary>Microsoft OneNote icon.</summary>
    public const string MicrosoftOneNote = "Microsoft OneNote";

    /// <summary>Microsoft SharePoint icon.</summary>
    public const string MicrosoftSharePoint = "Microsoft SharePoint";

    /// <summary>Microsoft Visio icon.</summary>
    public const string MicrosoftVisio = "Microsoft Visio";

    /// <summary>Microsoft Loop icon.</summary>
    public const string MicrosoftLoop = "Microsoft Loop";

    /// <summary>Microsoft Whiteboard icon.</summary>
    public const string MicrosoftWhiteboard = "Microsoft Whiteboard";

    /// <summary>Adobe Illustrator icon.</summary>
    public const string AdobeIllustrator = "Adobe Illustrator";

    /// <summary>Adobe Photoshop icon.</summary>
    public const string AdobePhotoshop = "Adobe Photoshop";

    /// <summary>Adobe InDesign icon.</summary>
    public const string AdobeInDesign = "Adobe InDesign";

    /// <summary>Adobe Flash icon.</summary>
    public const string AdobeFlash = "Adobe Flash";

    /// <summary>Sketch icon.</summary>
    public const string Sketch = "Sketch";

    /// <summary>Source code icon.</summary>
    public const string SourceCode = "Source Code";

    /// <summary>Image icon.</summary>
    public const string Image = "Image";

    /// <summary>GIF icon.</summary>
    public const string Gif = "GIF";

    /// <summary>Video icon.</summary>
    public const string Video = "Video";

    /// <summary>Sound icon.</summary>
    public const string Sound = "Sound";

    /// <summary>ZIP icon.</summary>
    public const string Zip = "ZIP";

    /// <summary>Text icon.</summary>
    public const string Text = "Text";

    /// <summary>PDF icon.</summary>
    public const string Pdf = "PDF";
}

/// <summary>
/// Known encoding format MIME types for citation documents.
/// </summary>
public static class EncodingFormats
{
    /// <summary>Adaptive card encoding format.</summary>
    public const string AdaptiveCard = "application/vnd.microsoft.card.adaptive";
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
    public string? EncodingFormat { get; set; }

    /// <summary>
    /// Gets or sets the citation icon name. See <see cref="CitationIcon"/> for known values.
    /// </summary>
    public string? Icon { get; set; }

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
