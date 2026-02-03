// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;

namespace Microsoft.Teams.Bot.Compat
{
    /// <summary>
    /// Defines an adapter interface for compatibility with Teams bots.
    /// </summary>
    public interface ICompatAdapter : IBotFrameworkHttpAdapter
    {
        /// <summary>
        /// Continues a conversation with the specified bot and conversation reference.
        /// </summary>
        /// <param name="botId">The bot identifier.</param>
        /// <param name="reference">The conversation reference.</param>
        /// <param name="callback">The bot callback handler to execute.</param>
        /// <param name="cancellationToken">A cancellation token for the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task ContinueConversationAsync(string botId, ConversationReference reference, BotCallbackHandler callback, CancellationToken cancellationToken);
    }
}
