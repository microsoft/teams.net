using System.Text.Json.Serialization;

namespace Microsoft.Teams.Schema
{
    public class TeamsChannelDataTenant
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }
}
