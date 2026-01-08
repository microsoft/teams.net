// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.BotApps.Schema;

/// <summary>
/// Provides a fluent API for creating <see cref="TeamsAttachment"/> instances.
/// </summary>
public class TeamsAttachmentBuilder
{
    private const string AdaptiveCardContentType = "application/vnd.microsoft.card.adaptive";

    private readonly TeamsAttachment _attachment;

    internal TeamsAttachmentBuilder() : this(new TeamsAttachment())
    {
    }

    internal TeamsAttachmentBuilder(TeamsAttachment attachment)
    {
        _attachment = attachment ?? throw new ArgumentNullException(nameof(attachment));
    }

    /// <summary>
    /// Sets the content type for the attachment.
    /// </summary>
    public TeamsAttachmentBuilder WithContentType(string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("Content type cannot be null or whitespace.", nameof(contentType));
        }

        _attachment.ContentType = contentType;
        return this;
    }

    /// <summary>
    /// Sets the payload for the attachment.
    /// </summary>
    public TeamsAttachmentBuilder WithContent(object? content)
    {
        _attachment.Content = content;
        return this;
    }

    /// <summary>
    /// Sets the content url for the attachment.
    /// </summary>
    public TeamsAttachmentBuilder WithContentUrl(Uri? contentUrl)
    {
        _attachment.ContentUrl = contentUrl;
        return this;
    }

    /// <summary>
    /// Sets the friendly name for the attachment.
    /// </summary>
    public TeamsAttachmentBuilder WithName(string? name)
    {
        _attachment.Name = name;
        return this;
    }

    /// <summary>
    /// Sets the thumbnail url for the attachment.
    /// </summary>
    public TeamsAttachmentBuilder WithThumbnailUrl(Uri? thumbnailUrl)
    {
        _attachment.ThumbnailUrl = thumbnailUrl;
        return this;
    }

    /// <summary>
    /// Adds or updates an extension property on the attachment.
    /// Passing a null value removes the property.
    /// </summary>
    public TeamsAttachmentBuilder WithProperty(string propertyName, object? value)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            throw new ArgumentException("Property name cannot be null or whitespace.", nameof(propertyName));
        }

        if (value is null)
        {
            _attachment.Properties.Remove(propertyName);
        }
        else
        {
            _attachment.Properties[propertyName] = value;
        }

        return this;
    }

    /// <summary>
    /// Configures the attachment to contain an Adaptive Card payload.
    /// </summary>
    public TeamsAttachmentBuilder WithAdaptiveCard(object adaptiveCard)
    {
        ArgumentNullException.ThrowIfNull(adaptiveCard);
        _attachment.ContentType = AdaptiveCardContentType;
        _attachment.Content = adaptiveCard;
        _attachment.ContentUrl = null;
        return this;
    }

    /// <summary>
    /// Builds the attachment.
    /// </summary>
    public TeamsAttachment Build() => _attachment;
}
