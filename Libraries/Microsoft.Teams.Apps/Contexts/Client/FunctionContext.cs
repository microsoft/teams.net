// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Clients;
using Microsoft.Teams.Cards;
using Microsoft.Teams.Common.Logging;

namespace Microsoft.Teams.Apps;

/// <summary>
/// context that comes from client (tab/embed) requests
/// for remote function calls
/// </summary>
public interface IFunctionContext<T> : IClientContext
{
    /// <summary>
    /// the api client
    /// </summary>
    public ApiClient Api { get; }

    /// <summary>
    /// the app logger instance
    /// </summary>
    public ILogger Log { get; }

    /// <summary>
    /// the function payload
    /// </summary>
    public T Data { get; }

    /// <summary>
    /// send an activity to the conversation
    /// </summary>
    /// <param name="activity">activity activity to send</param>
    /// <param name="cancellationToken">optional cancellation token</param>
    public Task<TActivity> Send<TActivity>(TActivity activity, CancellationToken cancellationToken = default) where TActivity : IActivity;

    /// <summary>
    /// send a message activity to the conversation
    /// </summary>
    /// <param name="text">the text to send</param>
    /// <param name="cancellationToken">optional cancellation token</param>
    public Task<MessageActivity> Send(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// send a message activity with a card attachment
    /// </summary>
    /// <param name="card">the card to send as an attachment</param>
    /// <param name="cancellationToken">optional cancellation token</param>
    public Task<MessageActivity> Send(AdaptiveCard card, CancellationToken cancellationToken = default);
}

/// <summary>
/// context that comes from client (tab/embed) requests
/// for remote function calls
/// </summary>
public class FunctionContext<T>(App app) : ClientContext, IFunctionContext<T>
{
    public required ApiClient Api { get; set; }
    public required ILogger Log { get; set; }
    public required T Data { get; set; }

    public async Task<TActivity> Send<TActivity>(TActivity activity, CancellationToken cancellationToken = default) where TActivity : IActivity
    {
        var conversationId = ConversationId ?? activity.Conversation?.Id;

        // Conversation ID can be missing if the app is running in a personal scope. In this case, create
        // a conversation between the bot and the user. This will either create a new conversation or return
        // a pre-existing one.
        if (conversationId is null)
        {
            var res = await Api.Conversations.CreateAsync(new()
            {
                TenantId = TenantId,
                IsGroup = false,
                Bot = new()
                {
                    Id = app.Id,
                    Name = app.Name,
                    Role = Role.Bot
                },
                Members = [
                    new()
                    {
                        Id = UserId,
                        Name = UserName,
                        Role = Role.User,
                    }
                ]
            });

            conversationId = res.Id;
        }

        return await app.Send(conversationId, activity, cancellationToken: cancellationToken);
    }

    public Task<MessageActivity> Send(string text, CancellationToken cancellationToken = default)
    {
        return Send(new MessageActivity(text), cancellationToken);
    }

    public Task<MessageActivity> Send(AdaptiveCard card, CancellationToken cancellationToken = default)
    {
        return Send(new MessageActivity().AddAttachment(card), cancellationToken);
    }
}