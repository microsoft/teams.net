// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.AI;

/// <summary>
/// used to handler streamed chunks of text
/// </summary>
/// <param name="text">the text chunk</param>
public delegate Task OnStreamChunk(string text);

/// <summary>
/// represents a stream
/// </summary>
public interface IStream
{
    /// <summary>
    /// emit a text chunk
    /// </summary>
    /// <param name="text">the text chunk</param>
    public void Emit(string text);
}

/// <summary>
/// Streams text chunks
/// </summary>
public class Stream(OnStreamChunk onChunk) : IStream
{
    public void Emit(string text)
    {
        onChunk(text).GetAwaiter().GetResult();
    }
}