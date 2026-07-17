// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Teams.Core.Schema;

/// <summary>
/// Provides a fluent API for building outbound <see cref="CoreActivityInput"/> instances.
/// </summary>
/// <typeparam name="TActivity">The type of activity being built.</typeparam>
/// <typeparam name="TBuilder">The type of the builder (for fluent method chaining).</typeparam>
public abstract class CoreActivityInputBuilder<TActivity, TBuilder>
    where TActivity : CoreActivityInput
    where TBuilder : CoreActivityInputBuilder<TActivity, TBuilder>
{
    /// <summary>
    /// The activity being built.
    /// </summary>
#pragma warning disable CA1051 // Do not declare visible instance fields
    protected readonly TActivity _activity;
#pragma warning restore CA1051 // Do not declare visible instance fields

    /// <summary>
    /// Initializes a new instance of the builder.
    /// </summary>
    /// <param name="activity">The activity to build upon.</param>
    protected CoreActivityInputBuilder(TActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        _activity = activity;
    }

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
    /// Sets the recipient account for the activity.
    /// </summary>
    /// <param name="account">The recipient account.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TBuilder WithRecipient(ChannelAccount account)
    {
        ArgumentNullException.ThrowIfNull(account);
        _activity.Recipient = account;
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
    /// Builds and returns the configured activity instance.
    /// </summary>
    /// <returns>The configured activity.</returns>
    public abstract TActivity Build();
}

/// <summary>
/// Provides a fluent API for building outbound <see cref="CoreActivityInput"/> instances.
/// </summary>
public class CoreActivityInputBuilder : CoreActivityInputBuilder<CoreActivityInput, CoreActivityInputBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CoreActivityInputBuilder"/> class.
    /// </summary>
    internal CoreActivityInputBuilder() : base(new CoreActivityInput())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CoreActivityInputBuilder"/> class with an existing activity.
    /// </summary>
    /// <param name="activity">The activity to build upon.</param>
    internal CoreActivityInputBuilder(CoreActivityInput activity) : base(activity)
    {
    }

    /// <summary>
    /// Builds and returns the configured <see cref="CoreActivityInput"/> instance.
    /// </summary>
    /// <returns>The configured <see cref="CoreActivityInput"/>.</returns>
    public override CoreActivityInput Build()
    {
        return _activity;
    }
}
