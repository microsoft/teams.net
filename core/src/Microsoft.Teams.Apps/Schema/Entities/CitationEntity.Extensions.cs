// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Apps.Schema.Entities;

/// <summary>
/// Citation entity extension methods.
/// </summary>
public static class CitationEntityExtensions
{
    /// <summary>
    /// Gets the first citation entity from the activity.
    /// </summary>
    public static CitationEntity? GetCitation(this TeamsActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        if (activity.Entities == null)
        {
            return null;
        }

        return activity.Entities.FirstOrDefault(e => e is CitationEntity) as CitationEntity;
    }

    /// <summary>
    /// Internal helper to add a citation claim to an activity.
    /// </summary>
    internal static CitationEntity AddToActivity(TeamsActivity activity, int position, CitationAppearance appearance)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(appearance);

        activity.Entities ??= [];

        // Get or create the root message entity
        OMessageEntity existingMessageEntity = OMessageEntityExtensions.GetOrCreateRootMessageEntity(activity);

        // Remove existing message entity to replace with citation entity
        activity.Entities.Remove(existingMessageEntity);

        // Create citation entity from message entity
        CitationEntity citationEntity = new(existingMessageEntity);
        citationEntity.Citation ??= [];
        citationEntity.Citation.Add(new CitationClaim()
        {
            Position = position,
            Appearance = appearance.ToDocument()
        });

        activity.Entities.Add(citationEntity);
        return citationEntity;
    }
}
