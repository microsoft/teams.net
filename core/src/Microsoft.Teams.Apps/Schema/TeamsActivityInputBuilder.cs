// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Teams.Apps.Schema.Entities;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.Schema;

/// <summary>
/// Provides the shared fluent API for builders of outbound <see cref="TeamsActivityInput"/> subtypes
/// (for example <see cref="Microsoft.Teams.Apps.MessageActivityInputBuilder"/> and <see cref="Microsoft.Teams.Apps.StreamingActivityInputBuilder"/>).
/// <para>
/// Inherits the common activity-level builder surface (id, type, recipient, properties) from
/// <see cref="CoreActivityInputBuilder{TActivity, TBuilder}"/> and adds Teams-specific entity helpers.
/// </para>
/// </summary>
/// <typeparam name="TActivity">The concrete <see cref="TeamsActivityInput"/> type being built.</typeparam>
/// <typeparam name="TBuilder">The concrete builder type (for fluent chaining).</typeparam>
public abstract class TeamsActivityInputBuilder<TActivity, TBuilder> : CoreActivityInputBuilder<TActivity, TBuilder>
    where TActivity : TeamsActivityInput
    where TBuilder : TeamsActivityInputBuilder<TActivity, TBuilder>
{
    /// <summary>
    /// Initializes a new instance of the builder.
    /// </summary>
    /// <param name="activity">The activity to build upon.</param>
    protected TeamsActivityInputBuilder(TActivity activity) : base(activity)
    {
    }

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
        _activity.Entities ??= [];
        _activity.Entities.Remove(oldEntity);
        _activity.Entities.Add(newEntity);
        return (TBuilder)this;
    }
}
