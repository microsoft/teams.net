using Microsoft.Teams.Api;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Entities;
using Microsoft.Teams.Apps.Plugins;
using Microsoft.Teams.Common.Extensions;

namespace Microsoft.Teams.Plugins.AspNetCore;

public partial class AspNetCorePlugin
{
    public class Stream : IStreamer
    {
        public bool Closed => _closedAt != null;
        public int Count => _count;
        public int Sequence => _index;

        public required Func<IActivity, Task<IActivity>> Send { get; set; }
        public event IStreamer.OnChunkHandler OnChunk = (_) => { };

        protected int _index = 1;
        protected string? _id;
        protected string _text = string.Empty;
        protected ChannelData _channelData = new();
        protected List<Attachment> _attachments = [];
        protected List<IEntity> _entities = [];
        protected Queue<IActivity> _queue = [];

        private DateTime? _closedAt;
        private int _count = 0;
        private MessageActivity? _result;
        private readonly Lock _lock = new();

        public void Emit(MessageActivity activity)
        {
            _queue.Enqueue(activity);
            Task.Run(Flush);
        }

        public void Emit(TypingActivity activity)
        {
            _queue.Enqueue(activity);
            Task.Run(Flush);
        }

        public void Emit(string text)
        {
            Emit(new MessageActivity(text));
        }

        public async Task<MessageActivity?> Close()
        {
            if (_index == 0) return null;
            if (_result != null) return _result;
            while (_id == null || _queue.Count > 0)
            {
                await Task.Delay(50);
            }

            var activity = new MessageActivity(_text)
                .AddAttachment(_attachments.ToArray());

            activity.WithId(_id);
            activity.WithData(_channelData);
            activity.AddEntity(_entities.ToArray());
            activity.AddStreamFinal();

            var res = await Send(activity).Retry();
            OnChunk(res);

            _result = activity;
            _closedAt = DateTime.Now;
            _index = 1;
            _id = null;
            _text = string.Empty;
            _attachments = [];
            _entities = [];
            _channelData = new();

            return (MessageActivity)res;
        }

        protected Task Flush()
        {
            if (_queue.Count == 0) return Task.CompletedTask;

            lock (_lock)
            {
                if (_queue.Count == 0) return Task.CompletedTask;
                var i = 0;

                while (i <= 10 && _queue.TryDequeue(out var activity))
                {
                    if (activity is MessageActivity message)
                    {
                        _text += message.Text;
                        _attachments.AddRange(message.Attachments ?? []);
                        _entities.AddRange(message.Entities ?? []);
                    }

                    if (activity.ChannelData != null)
                    {
                        _channelData = _channelData.Merge(activity.ChannelData);
                    }

                    i++;
                    _count++;
                }

                if (i == 0) return Task.CompletedTask;

                var toSend = new TypingActivity(_text);

                if (_id != null)
                {
                    toSend.WithId(_id);
                }

                toSend.AddStreamUpdate(_index);
                var res = Send(toSend).Retry(delay: 10).ConfigureAwait(false).GetAwaiter().GetResult();
                OnChunk(res);
                _id ??= res.Id;
                _index++;
            }

            return Task.CompletedTask;
        }
    }
}