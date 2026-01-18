// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Teams.Bot.Core.Schema;
using Microsoft.Teams.Bot.Apps.Schema;

namespace Microsoft.Teams.Bot.Apps.Handlers;

/// <summary>
/// Delegate for handling conversation update activities when members are added or removed from a conversation.
/// </summary>
/// <param name="conversationUpdateActivity">The conversation update arguments containing member changes and activity details.</param>
/// <param name="context">The turn context for sending responses and accessing conversation information.</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
/// <returns>A task that represents the asynchronous handler operation.</returns>
public delegate Task ConversationUpdateHandler(ConversationUpdateArgs conversationUpdateActivity, Context context, CancellationToken cancellationToken = default);

/// <summary>
/// Provides arguments for conversation update activities including members added and removed.
/// </summary>
/// <param name="act">The Teams activity containing the conversation update information.</param>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227: Collection Properties should be read only", Justification = "<Pending>")]
public class ConversationUpdateArgs(TeamsActivity act)
{
    /// <summary>
    /// Activity for the conversation update.
    /// </summary>
    public TeamsActivity Activity { get; set; } = act;

    /// <summary>
    /// Members added to the conversation.
    /// </summary>
    public IList<ConversationAccount>? MembersAdded { get; set; } =
        act.Properties.TryGetValue("membersAdded", out object? value)
            && value is JsonElement je
            && je.ValueKind == JsonValueKind.Array
                ? JsonSerializer.Deserialize<IList<ConversationAccount>>(je.GetRawText())
                : null;

    /// <summary>
    /// Members removed from the conversation.  
    /// </summary>
    public IList<ConversationAccount>? MembersRemoved { get; set; } =
        act.Properties.TryGetValue("membersRemoved", out object? value2)
            && value2 is JsonElement je2
            && je2.ValueKind == JsonValueKind.Array
                ? JsonSerializer.Deserialize<IList<ConversationAccount>>(je2.GetRawText())
                : null;
}
