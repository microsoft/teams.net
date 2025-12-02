using System.Text.Json.Serialization;

namespace Microsoft.Teams.Schema
{
    public class Team
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        [JsonPropertyName("aadGroupId")]
        public string? AadGroupId { get; set; }
        [JsonPropertyName("tenantId")]
        public string? TenantId { get; set; }
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("channelCount")]
        public int? ChannelCount { get; set; }

        [JsonPropertyName("memberCount")]
        public int? MemberCount { get; set; }
    }
}
