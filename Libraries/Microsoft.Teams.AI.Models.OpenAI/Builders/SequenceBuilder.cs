// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Buffers;
using System.Diagnostics;

namespace Microsoft.Teams.AI.Models.OpenAI.Builders;

/// <summary>
/// https://github.com/openai/openai-dotnet/blob/main/examples/Chat/Example04_FunctionCallingStreaming.cs#L74
/// </summary>
public class SequenceBuilder<T>
{
    Segment? _first;
    Segment? _last;

    public void Append(ReadOnlyMemory<T> data)
    {
        if (_first is null)
        {
            Debug.Assert(_last is null);
            _first = new Segment(data);
            _last = _first;
        }
        else
        {
            _last = _last!.Append(data);
        }
    }

    public ReadOnlySequence<T> Build()
    {
        if (_first is null)
        {
            Debug.Assert(_last is null);
            return ReadOnlySequence<T>.Empty;
        }

        if (_first == _last)
        {
            Debug.Assert(_first.Next is null);
            return new ReadOnlySequence<T>(_first.Memory);
        }

        return new ReadOnlySequence<T>(_first, 0, _last!, _last!.Memory.Length);
    }

    private sealed class Segment : ReadOnlySequenceSegment<T>
    {
        public Segment(ReadOnlyMemory<T> items) : this(items, 0)
        {
        }

        private Segment(ReadOnlyMemory<T> items, long runningIndex)
        {
            Debug.Assert(runningIndex >= 0);
            Memory = items;
            RunningIndex = runningIndex;
        }

        public Segment Append(ReadOnlyMemory<T> items)
        {
            long runningIndex;
            checked { runningIndex = RunningIndex + Memory.Length; }
            Segment segment = new(items, runningIndex);
            Next = segment;
            return segment;
        }
    }
}