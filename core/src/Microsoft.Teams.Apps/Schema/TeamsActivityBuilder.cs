// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Teams.Apps.Schema.Entities;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.Schema;

/// <summary>
/// Provides the shared fluent API for builders of <see cref="TeamsActivity"/> subtypes
/// (for example <see cref="MessageActivityBuilder"/> and <see cref="StreamingActivityBuilder"/>).
/// </summary>
/// <typeparam name="TActivity">The concrete <see cref="TeamsActivity"/> type being built.</typeparam>
/// <typeparam name="TBuilder">The concrete builder type (for fluent chaining).</typeparam>
public abstract class TeamsActivityBuilder<TActivity, TBuilder>
    where TActivity : TeamsActivity
    where TBuilder : TeamsActivityBuilder<TActivity, TBuilder>
{
    /// <summary>
    /// The activity being built.
    /// </summary>
#pragma warning disable CA1051 // Do not declare visible instance fields
    protected readonly TActivity _activity;
#pragma warning restore CA1051 // Do not declare visible instance fields

    /// <summary>
    /// Initializes a new instance of the <see cref="TeamsActivityBuilder{TActivity, TBuilder}"/> class.
    /// </summary>
    /// <param name="activity">The activity to build upon.</param>
    protected TeamsActivityBuilder(TActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        _activity = activity;
    }

    // ==================== Activity metadata ====================

    /// <summary>
    /// Sets the activity ID.
    /// </summary>
    public TBuilder WithId(string id)
    {
        _activity.Id = id;
        return (TBuilder)this;
    }

    /// <summary>
    /// Adds or updates a property in the activity's extension Properties dictionary.
    /// </summary>
    public TBuilder WithProperty<T>(string name, T? value)
    {
        _activity.Properties[name] = value;
        return (TBuilder)this;
    }

    /// <summary>
    /// Builds and returns the configured activity instance.
    /// </summary>
    public abstract TActivity Build();

    // ==================== Entities ====================

    /// <summary>
    /// Sets the entities collection.
    /// </summary>
    public TBuilder WithEntities(EntityList entities)
    {
        _activity.Entities = entities;
        return (TBuilder)this;
    }

    /// <summary>
    /// Adds one or more entities to the activity.
    /// </summary>
    public TBuilder AddEntity(params Entity[] entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        _activity.Entities ??= [];
        foreach (Entity entity in entities)
        {
            _activity.Entities.Add(entity);
        }
        return (TBuilder)this;
    }

    /// <summary>
    /// Replaces an existing entity with a new entity.
    /// </summary>
    public TBuilder UpdateEntity(Entity oldEntity, Entity newEntity)
    {
        ArgumentNullException.ThrowIfNull(oldEntity);
        ArgumentNullException.ThrowIfNull(newEntity);
        if (_activity.Entities is not null)
        {
            _activity.Entities.Remove(oldEntity);
        }
        else
        {
            _activity.Entities = [];
        }
        _activity.Entities.Add(newEntity);
        return (TBuilder)this;
    }

    // ==================== Suggested actions ====================

    /// <summary>
    /// Sets the suggested actions.
    /// </summary>
    public TBuilder WithSuggestedActions(SuggestedActions suggestedActions)
    {
        _activity.SuggestedActions = suggestedActions;
        return (TBuilder)this;
    }

    // ==================== Mentions / quotes / citations / feedback ====================

    /// <summary>
    /// Adds a mention (@mention) entity and optionally prepends mention text.
    /// </summary>
    public TBuilder AddMention(ChannelAccount account, string? text = null, bool addText = true)
    {
        ArgumentNullException.ThrowIfNull(account);
        MentionEntityExtensions.AddToActivity(_activity, account, text, addText);
        return (TBuilder)this;
    }

    /// <summary>
    /// Adds a quoted message reference and appends a placeholder to the activity text.
    /// </summary>
    public TBuilder AddQuote(string messageId, string? text = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        QuotedReplyEntityExtensions.AddToActivity(_activity, messageId, text);
        return (TBuilder)this;
    }

    /// <summary>
    /// Adds a targetedMessageInfo entity for Prompt Preview, referencing the inbound targeted-message id.
    /// </summary>
    [Experimental("ExperimentalTeamsTargeted")]
    public TBuilder WithTargetedMessageInfo(string messageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        TargetedMessageInfoEntityExtensions.AddToActivity(_activity, messageId);
        return (TBuilder)this;
    }

    /// <summary>
    /// Adds a clientInfo entity to the activity.
    /// </summary>
    public TBuilder AddClientInfo(string? platform, string? country, string? timezone, string? locale)
    {
        ClientInfoEntityExtensions.AddToActivity(_activity, platform, country, timezone, locale);
        return (TBuilder)this;
    }

    /// <summary>
    /// Adds a productInfo entity to the activity.
    /// </summary>
    public TBuilder AddProductInfo(string? id)
    {
        ProductInfoEntityExtensions.AddToActivity(_activity, id);
        return (TBuilder)this;
    }

    /// <summary>
    /// Adds the AI-generated content label to the root message entity.
    /// </summary>
    public TBuilder AddAIGenerated()
    {
        OMessageEntityExtensions.AddAIGeneratedContent(_activity);
        return (TBuilder)this;
    }

    /// <summary>
    /// Enables/disables the feedback loop on the activity.
    /// </summary>
    public TBuilder AddFeedback(bool value = true)
    {
        _activity.ChannelData ??= new TeamsChannelData();
        _activity.ChannelData.FeedbackLoopEnabled = value;
        return (TBuilder)this;
    }

    /// <summary>
    /// Configures the feedback loop mode on the activity.
    /// </summary>
    public TBuilder AddFeedback(string mode)
    {
        _activity.ChannelData ??= new TeamsChannelData();
        _activity.ChannelData.FeedbackLoop = new FeedbackLoop(mode);
        _activity.ChannelData.FeedbackLoopEnabled = null;
        return (TBuilder)this;
    }

    /// <summary>
    /// Adds a citation claim to the activity.
    /// </summary>
    public TBuilder AddCitation(int position, CitationAppearance appearance)
    {
        ArgumentNullException.ThrowIfNull(appearance);
        _activity.Entities ??= [];
        CitationEntityExtensions.AddToActivity(_activity, position, appearance);
        return (TBuilder)this;
    }

    /// <summary>
    /// Adds a content sensitivity label to the activity.
    /// </summary>
    public TBuilder AddSensitivityLabel(string name, string? description = null, DefinedTerm? pattern = null)
    {
        SensitiveUsageEntityExtensions.AddToActivity(_activity, name, description, pattern);
        return (TBuilder)this;
    }
}
