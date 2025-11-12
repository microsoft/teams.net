using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Plugins;

namespace Microsoft.Teams.Plugins.Agents;

public partial class TeamsAgentPlugin
{
    public class Stream(Microsoft.Agents.Builder.ITurnContext context) : IStreamer
    {
        public bool Closed => _closedAt is not null;
        public int Count => _count;
        public int Sequence => _count;

        public int _count = 0;
        public DateTime? _closedAt;

        public event IStreamer.OnChunkHandler OnChunk = (_) => { };

        public void Emit(MessageActivity activity)
        {
            _count++;
            context.StreamingResponse.QueueTextChunk(activity.Text);
        }

        public void Emit(TypingActivity activity)
        {
            _count++;
            context.StreamingResponse.QueueInformativeUpdateAsync(activity.Text ?? string.Empty);
        }

        public void Emit(string text)
        {
            _count++;
            context.StreamingResponse.QueueTextChunk(text);
        }

        public void Update(string text)
        {
            _count++;
            context.StreamingResponse.QueueInformativeUpdateAsync(text);
        }

        public async Task<MessageActivity?> Close()
        {
            _closedAt = new DateTime();
            if (!context.StreamingResponse.IsStreamStarted()) return null;
            await context.StreamingResponse.EndStreamAsync();
            return ((Activity)context.StreamingResponse.FinalMessage.ToTeams()).ToMessage();
        }
    }
}