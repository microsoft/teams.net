using Microsoft.Bot.Core.Schema;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Schema
{
    public class TeamsConversation : Conversation
    {
        public TeamsConversation(Conversation conversation)
        {
            Id = conversation.Id ?? string.Empty;
            if (conversation.Properties == null)
            {
                return;
            }
            if (conversation.Properties.TryGetValue("tenantId", out object? tenantObj) && tenantObj is JsonElement je && je.ValueKind == JsonValueKind.String)
            {
                TenantId = je.GetString();
            }
            if (conversation.Properties.TryGetValue("conversationType", out object? convTypeObj) && convTypeObj is JsonElement je2 && je2.ValueKind == JsonValueKind.String)
            {
                ConversationType = je2.GetString();
            }
        }

        [JsonPropertyName("tenantId")]
        public string? TenantId { get; set; }

        [JsonPropertyName("conversationType")]
        public string? ConversationType { get; set; }
    }
}
