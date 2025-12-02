using Microsoft.Bot.Core.Schema;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Schema;

public class TeamsConversationAccount : ConversationAccount
{
    public TeamsConversationAccount(ConversationAccount ca)
    {
        Id = ca?.Id ?? string.Empty;
        Name = ca?.Name ?? string.Empty;
        if (ca is not null && ca.Properties.TryGetValue("aadObjectId", out object? aadObj)
            && aadObj is JsonElement je
            && je.ValueKind == JsonValueKind.String)
        {
            AadObjectId = je.GetString();
        }
    }
    [JsonPropertyName("aadObjectId")]
    public string? AadObjectId { get; set; }
}
