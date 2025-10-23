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
        public Task<string?> SignIn(OAuthOptions? options = null) => context.SignIn(options);

        /// <summary>
        /// trigger user SSO signin flow for the activity sender
        /// </summary>
        /// <param name="options">option overrides</param>
        public Task SignIn(SSOOptions options) => context.SignIn(options);

        /// <summary>
        /// trigger user signin flow for the activity sender
        /// </summary>
        /// <param name="connectionName">the connection name</param>
        public Task SignOut(string? connectionName = null) => context.SignOut(connectionName);

        /// <summary>
        /// Send a targeted activity to a specific user in the conversation
        /// </summary>
        /// <param name="userId">The user MRI of the targeted message recipient</param>
        /// <param name="activity">The activity to send as a targeted message</param>
        public Task<T> SendTargeted<T>(string userId, T activity) where T : IActivity => context.SendTargeted(userId, activity);

        /// <summary>
        /// Send a targeted message to a specific user in the conversation
        /// </summary>
        /// <param name="userId">The user MRI of the targeted message recipient</param>
        /// <param name="text">The text to send</param>
        public Task<MessageActivity> SendTargeted(string userId, string text) => context.SendTargeted(userId, text);

        /// <summary>
        /// Send a targeted message with a card attachment to a specific user
        /// </summary>
        /// <param name="userId">The user MRI of the targeted message recipient</param>
        /// <param name="card">The card to send as an attachment</param>
        public Task<MessageActivity> SendTargeted(string userId, Cards.AdaptiveCard card) => context.SendTargeted(userId, card);

        /// <summary>
        /// Update a previously sent targeted message
        /// </summary>
        /// <param name="userId">The user MRI of the targeted message recipient</param>
        /// <param name="activityId">The targeted message ID to update</param>
        /// <param name="activity">The updated activity</param>
        public Task<T> UpdateTargeted<T>(string userId, string activityId, T activity) where T : IActivity => context.UpdateTargeted(userId, activityId, activity);
    }

    /// <summary>
    /// calls the next handler in the route chain
    /// </summary>
    public delegate Task<object?> Next();
}