using Microsoft.Teams.Api;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Auth;

namespace Microsoft.Teams.Apps.Plugins;

/// <summary>
/// a plugin that can send activities
/// </summary>
public interface ISenderPlugin : IPlugin
{
    /// <summary>
    /// emitted when the plugin receives an activity
    /// </summary>
    public event ActivityEventHandler ActivityEvent;

    /// <summary>
    /// called by the `App`
    /// to send an activity
    /// </summary>
    /// <param name="activity">the activity to send</param>
    /// <param name="reference">the conversation reference</param>
    /// <returns>the sent activity</returns>
    public Task<IActivity> Send(IActivity activity, ConversationReference reference, CancellationToken cancellationToken = default);

    /// <summary>
    /// called by the `App`
    /// to send an activity
    /// </summary>
    /// <typeparam name="TActivity">the activity type</typeparam>
    /// <param name="activity">the activity to send</param>
    /// <param name="reference">the conversation reference</param>
    /// <returns>the sent activity</returns>
    public Task<TActivity> Send<TActivity>(TActivity activity, ConversationReference reference, CancellationToken cancellationToken = default) where TActivity : IActivity;

    /// <summary>
    /// called by the `App`
    /// to create a new activity stream
    /// </summary>
    /// <param name="reference">the conversation reference</param>
    /// <returns>a new stream</returns>
    public IStreamer CreateStream(ConversationReference reference, CancellationToken cancellationToken = default);

    /// <summary>
    /// process an activity
    /// </summary>
    public Task<Response> Do(IToken token, IActivity activity, CancellationToken cancellationToken = default);

    public delegate Task<Response> ActivityEventHandler(ISenderPlugin sender, IToken token, IActivity activity, CancellationToken cancellationToken = default);
}