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
    internal static StreamInfoEntity AddToActivity(TeamsActivityInput activity, StreamType streamType, string? streamId = null, int? streamSequence = null)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(streamType);

        activity.ChannelData ??= new TeamsOutboundChannelData();

        string? resolvedStreamId = streamId;
        if (string.IsNullOrWhiteSpace(resolvedStreamId))
        {
            resolvedStreamId = activity.ChannelData.StreamId ?? activity.Id;
        }

        activity.ChannelData.StreamId = resolvedStreamId;
        activity.ChannelData.StreamType = streamType;
        if (streamSequence.HasValue)
        {
            activity.ChannelData.StreamSequence = streamSequence.Value;
        }

        activity.Entities ??= [];
        StreamInfoEntity entity = new()
        {
            StreamId = resolvedStreamId,
            StreamType = streamType,
            StreamSequence = streamSequence
        };

        activity.Entities.Add(entity);
        return entity;
    }


}
