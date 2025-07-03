// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;

namespace Microsoft.Teams.Apps.Plugins;

/// <summary>
/// component that can send streamed chunks of an activity
/// </summary>
public interface IStreamer
{
    /// <summary>
    /// whether the final stream
    /// message has been sent
    /// </summary>
    public bool Closed { get; }

    /// <summary>
    /// the total number of chunks queued to be sent
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// the sequence number, representing the
    /// number of stream activities sent
    /// </summary>
    /// <remarks>
    /// several chunks can be aggregated into one
    /// stream activity due to differences in Api rate limits
    /// </remarks>
    public int Sequence { get; }

    /// <summary>
    /// event emitted on each chunk send
    /// </summary>
    public event OnChunkHandler OnChunk;

    /// <summary>
    /// emit an activity
    /// </summary>
    /// <param name="activity">the activity</param>
    public void Emit(MessageActivity activity);

    /// <summary>
    /// emit an activity
    /// </summary>
    /// <param name="activity">the activity</param>
    public void Emit(TypingActivity activity);

    /// <summary>
    /// emit text chunk
    /// </summary>
    /// <param name="text">the text</param>
    public void Emit(string text);

    /// <summary>
    /// send status updates before emitting (ex. "Thinking...")
    /// </summary>
    /// <param name="text">the text</param>
    public void Update(string text);

    /// <summary>
    /// close the stream
    /// </summary>
    public Task<MessageActivity?> Close();

    /// <summary>
    /// handler called on each chunk send
    /// </summary>
    public delegate void OnChunkHandler(IActivity activity);
}