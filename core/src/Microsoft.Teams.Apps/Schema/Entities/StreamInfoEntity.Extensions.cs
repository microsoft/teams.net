// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Apps.Schema.Entities;

/// <summary>
/// Stream info entity extension methods.
/// </summary>
public static class StreamInfoEntityExtensions
{
    /// <summary>
    /// Gets the first stream info entity from the activity.
    /// </summary>
    public static StreamInfoEntity? GetStreamInfo(this TeamsActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        if (activity.Entities == null)
        {
            return null;
        }

        return activity.Entities.FirstOrDefault(e => e is StreamInfoEntity) as StreamInfoEntity;
    }

    /// <summary>
    /// Internal helper to add stream info to an activity for both streaming and final flow.
    /// </summary>
    internal static StreamInfoEntity AddToActivity(TeamsActivity activity, string streamType, string? streamId = null, int? streamSequence = null)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentException.ThrowIfNullOrWhiteSpace(streamType);

        activity.ChannelData ??= new TeamsChannelData();

        string? resolvedStreamId = streamId;
        if (string.IsNullOrWhiteSpace(resolvedStreamId))
        {
            if (activity.ChannelData.Properties.TryGetValue("streamId", out object? existingStreamId) && existingStreamId is not null)
            {
                resolvedStreamId = existingStreamId.ToString();
            }
            else
            {
                resolvedStreamId = activity.Id;
            }
        }

        activity.ChannelData.Properties["streamId"] = resolvedStreamId;
        activity.ChannelData.Properties["streamType"] = streamType;
        if (streamSequence.HasValue)
        {
            activity.ChannelData.Properties["streamSequence"] = streamSequence.Value;
        }

        activity.Entities ??= [];
        StreamInfoEntity entity = new()
        {
            StreamId = resolvedStreamId,
            StreamTypes = streamType,
            StreamSequence = streamSequence
        };

        activity.Entities.Add(entity);
        return entity;
    }


}
