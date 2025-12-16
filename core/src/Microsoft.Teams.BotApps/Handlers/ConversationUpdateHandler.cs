using System.Text.Json;
using Microsoft.Bot.Core.Schema;
using Microsoft.Teams.BotApps.Schema;

namespace Microsoft.Teams.BotApps.Handlers;

/// <summary>
/// Delegate for handling conversation update activities.
/// </summary>
/// <param name="conversationUpdateActivity"></param>
/// <param name="context"></param>
/// <param name="cancellationToken"></param>
/// <returns></returns>
public delegate Task ConversationUpdateHandler(ConversationUpdateArgs conversationUpdateActivity, Context context, CancellationToken cancellationToken = default);

/// <summary>
/// Conversation update activity arguments.
/// </summary>
/// <param name="act"></param>
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
