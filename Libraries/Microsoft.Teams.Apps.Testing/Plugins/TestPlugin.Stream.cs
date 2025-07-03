// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Entities;
using Microsoft.Teams.Apps.Plugins;

namespace Microsoft.Teams.Apps.Testing.Plugins;

public partial class TestPlugin
{
    /// <summary>
    /// a test implementation of an IStreamer
    /// </summary>
    public class Stream : IStreamer
    {
        public bool Closed { get; internal set; }
        public int Count { get; internal set; }
        public int Sequence { get; internal set; }

        public event IStreamer.OnChunkHandler OnChunk;

        protected MessageActivity? _activity;

        public void Emit(MessageActivity activity)
        {
            if (_activity is null)
            {
                _activity = activity;
            }

            _activity.Merge(activity);
        }

        public void Emit(TypingActivity activity)
        {
            if (_activity is null) return;
            _activity.Merge(activity);
        }

        public void Emit(string text)
        {
            Emit(new MessageActivity(text));
        }

        public void Update(string text)
        {
            Emit(new TypingActivity(text)
            {
                ChannelData = new()
                {
                    StreamType = StreamType.Informative
                }
            });
        }

        public Task<MessageActivity?> Close()
        {
            Closed = true;
            return Task.FromResult(_activity);
        }
    }
}