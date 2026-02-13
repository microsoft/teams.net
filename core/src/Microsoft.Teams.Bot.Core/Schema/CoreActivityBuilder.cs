// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Bot.Core.Schema;

/// <summary>
/// Provides a fluent API for building CoreActivity instances.
/// </summary>
/// <typeparam name="TActivity">The type of activity being built.</typeparam>
/// <typeparam name="TBuilder">The type of the builder (for fluent method chaining).</typeparam>
public abstract class CoreActivityBuilder<TActivity, TBuilder>
    where TActivity : CoreActivity
    where TBuilder : CoreActivityBuilder<TActivity, TBuilder>
{
    /// <summary>
    /// The activity being built.
    /// </summary>
#pragma warning disable CA1051 // Do not declare visible instance fields
    protected readonly TActivity _activity;
#pragma warning restore CA1051 // Do not declare visible instance fields

    /// <summary>
    /// Initializes a new instance of the CoreActivityBuilder class.
    /// </summary>
    /// <param name="activity">The activity to build upon.</param>
    protected CoreActivityBuilder(TActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        _activity = activity;
    }

    /// <summary>
    /// Apply Conversation Reference
    /// </summary>
    /// <param name="activity">The source activity to copy conversation reference from.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TBuilder WithConversationReference(TActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(activity.ChannelId);
        ArgumentNullException.ThrowIfNull(activity.ServiceUrl);
        ArgumentNullException.ThrowIfNull(activity.Conversation);
        ArgumentNullException.ThrowIfNull(activity.From);
        ArgumentNullException.ThrowIfNull(activity.Recipient);

        WithServiceUrl(activity.ServiceUrl);
        WithChannelId(activity.ChannelId);
        SetConversation(activity.Conversation);
        SetFrom(activity.Recipient);
        SetRecipient(activity.From);

        return (TBuilder)this;
    }

    /// <summary>
    /// Sets the conversation (to be overridden by derived classes for type-specific behavior).
    /// </summary>
    protected abstract void SetConversation(Conversation conversation);

    /// <summary>
    /// Sets the From account (to be overridden by derived classes for type-specific behavior).
    /// </summary>
    protected abstract void SetFrom(ConversationAccount from);

    /// <summary>
    /// Sets the Recipient account (to be overridden by derived classes for type-specific behavior).
    /// </summary>
    protected abstract void SetRecipient(ConversationAccount recipient);

    /// <summary>
    /// Sets the activity ID.
    /// </summary>
    /// <param name="id">The activity ID.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TBuilder WithId(string id)
    {
        _activity.Id = id;
        return (TBuilder)this;
    }

    /// <summary>
    /// Sets the service URL.
    /// </summary>
    /// <param name="serviceUrl">The service URL.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TBuilder WithServiceUrl(Uri serviceUrl)
    {
        _activity.ServiceUrl = serviceUrl;
        return (TBuilder)this;
    }

    /// <summary>
    /// Sets the channel ID.
    /// </summary>
    /// <param name="channelId">The channel ID.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TBuilder WithChannelId(string channelId)
    {
        _activity.ChannelId = channelId;
        return (TBuilder)this;
    }

    /// <summary>
    /// Sets the activity type.
    /// </summary>
    /// <param name="type">The activity type.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TBuilder WithType(string type)
    {
        _activity.Type = type;
        return (TBuilder)this;
    }

    /// <summary>
    /// Adds or updates a property in the activity's Properties dictionary.
    /// </summary>
    /// <param name="name">Name of the property.</param>
    /// <param name="value">Value of the property.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TBuilder WithProperty<T>(string name, T? value)
    {
        _activity.Properties[name] = value;
        return (TBuilder)this;
    }

    /// <summary>
    /// Sets the sender account information.
    /// </summary>
    /// <param name="from">The sender account.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TBuilder WithFrom(ConversationAccount from)
    {
        SetFrom(from);
        return (TBuilder)this;
    }

    /// <summary>
    /// Sets the recipient account information.
    /// </summary>
    /// <param name="recipient">The recipient account.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TBuilder WithRecipient(ConversationAccount recipient)
    {
        SetRecipient(recipient);
        return (TBuilder)this;
    }

    /// <summary>
    /// Sets the conversation information.
    /// </summary>
    /// <param name="conversation">The conversation information.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TBuilder WithConversation(Conversation conversation)
    {
        SetConversation(conversation);
        return (TBuilder)this;
    }

    /// <summary>
    /// Sets the channel-specific data (to be overridden by derived classes for type-specific behavior).
    /// </summary>
    /// <param name="channelData">The channel data.</param>
    /// <returns>The builder instance for chaining.</returns>
    public virtual TBuilder WithChannelData(ChannelData? channelData)
    {
        _activity.ChannelData = channelData;
        return (TBuilder)this;
    }

    /// <summary>
    /// Builds and returns the configured activity instance.
    /// </summary>
    /// <returns>The configured activity.</returns>
    public abstract TActivity Build();
}

/// <summary>
/// Provides a fluent API for building CoreActivity instances.
/// </summary>
public class CoreActivityBuilder : CoreActivityBuilder<CoreActivity, CoreActivityBuilder>
{
    /// <summary>
    /// Initializes a new instance of the CoreActivityBuilder class.
    /// </summary>
    internal CoreActivityBuilder() : base(new CoreActivity())
    {
    }

    /// <summary>
    /// Initializes a new instance of the CoreActivityBuilder class with an existing activity.
    /// </summary>
    /// <param name="activity">The activity to build upon.</param>
    internal CoreActivityBuilder(CoreActivity activity) : base(activity)
    {
    }

    /// <summary>
    /// Sets the conversation.
    /// </summary>
    protected override void SetConversation(Conversation conversation)
    {
        _activity.Conversation = conversation;
    }

    /// <summary>
    /// Sets the From account.
    /// </summary>
    protected override void SetFrom(ConversationAccount from)
    {
        _activity.From = from;
    }

    /// <summary>
    /// Sets the Recipient account.
    /// </summary>
    protected override void SetRecipient(ConversationAccount recipient)
    {
        _activity.Recipient = recipient;
    }

    /// <summary>
    /// Builds and returns the configured CoreActivity instance.
    /// </summary>
    /// <returns>The configured CoreActivity.</returns>
    public override CoreActivity Build()
    {
        return _activity;
    }
}
