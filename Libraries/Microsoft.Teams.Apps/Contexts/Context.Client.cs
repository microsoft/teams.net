// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;

namespace Microsoft.Teams.Apps;

public partial interface IContext
{
    /// <summary>
    /// an object that can send activities
    /// </summary>
    /// <param name="context">the parent context</param>
    public class Client(IContext<IActivity> context)
    {
        /// <summary>
        /// send an activity to the conversation
        /// </summary>
        /// <param name="activity">activity activity to send</param>
        public Task<T> Send<T>(T activity) where T : IActivity => context.Send(activity);

        /// <summary>
        /// send a message activity to the conversation
        /// </summary>
        /// <param name="text">the text to send</param>
        public Task<MessageActivity> Send(string text) => context.Send(text);

        /// <summary>
        /// send a message activity with a card attachment
        /// </summary>
        /// <param name="card">the card to send as an attachment</param>
        public Task<MessageActivity> Send(Cards.AdaptiveCard card) => context.Send(card);

        /// <summary>
        /// send an activity to the conversation as a reply
        /// </summary>
        /// <param name="activity">activity activity to send</param>
        public Task<T> Reply<T>(T activity) where T : IActivity => context.Reply(activity);

        /// <summary>
        /// send a message activity to the conversation as a reply
        /// </summary>
        /// <param name="text">the text to send</param>
        public Task<MessageActivity> Reply(string text) => context.Reply(text);

        /// <summary>
        /// send a message activity with a card attachment as a reply
        /// </summary>
        /// <param name="card">the card to send as an attachment</param>
        public Task<MessageActivity> Reply(Cards.AdaptiveCard card) => context.Reply(card);

        /// <summary>
        /// send a typing activity
        /// </summary>
        public Task<TypingActivity> Typing(string? text = null) => context.Typing(text);

        /// <summary>
        /// trigger user signin flow for the activity sender
        /// </summary>
        /// <param name="options">option overrides</param>
        /// <returns>the existing user token if found</returns>
        public Task<string?> SignIn(SignInOptions? options = null) => context.SignIn(options);

        /// <summary>
        /// trigger user signin flow for the activity sender
        /// </summary>
        /// <param name="connectionName">the connection name</param>
        public Task SignOut(string? connectionName = null) => context.SignOut(connectionName);
    }

    /// <summary>
    /// calls the next handler in the route chain
    /// </summary>
    public delegate Task<object?> Next();
}