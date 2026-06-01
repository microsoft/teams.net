// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Apps.Schema.Entities;

/// <summary>
/// OMessage entity extension methods.
/// </summary>
public static class OMessageEntityExtensions
{
    /// <summary>
    /// Gets the first message entity from the activity.
    /// </summary>
    public static OMessageEntity? GetMessageEntity(this TeamsActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        if (activity.Entities == null)
        {
            return null;
        }

        return activity.Entities.FirstOrDefault(e => e.Type == "https://schema.org/Message" && e is OMessageEntity) as OMessageEntity;
    }

    /// <summary>
    /// Internal helper to get or create the root message entity for an activity.
    /// </summary>
    internal static OMessageEntity GetOrCreateRootMessageEntity(TeamsActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

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

    /// <summary>
    /// Internal helper to add AI-generated content label to the message entity.
    /// </summary>
    internal static OMessageEntity AddAIGeneratedContent(TeamsActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        OMessageEntity messageEntity = GetOrCreateRootMessageEntity(activity);
        messageEntity.AdditionalType ??= [];
        if (!messageEntity.AdditionalType.Contains("AIGeneratedContent"))
        {
            messageEntity.AdditionalType.Add("AIGeneratedContent");
        }

        return messageEntity;
    }
}
