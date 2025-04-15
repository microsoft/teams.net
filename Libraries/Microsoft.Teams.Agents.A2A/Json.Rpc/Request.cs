using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Agents.A2A.Json.Rpc;

public class Request
{
    [JsonPropertyName("jsonrpc")]
    [JsonPropertyOrder(0)]
    public string Version => "2.0";

    [JsonPropertyName("id")]
    [JsonPropertyOrder(1)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("method")]
    [JsonPropertyOrder(2)]
    public string? Method { get; set; }

    [JsonPropertyName("params")]
    [JsonPropertyOrder(3)]
    public object? Params { get; set; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }

    public Response ToResponse()
    {
        return new() { Id = Id };
    }

    public static Request Notification(string method, object? args = null)
    {
        return new()
        {
            Method = method,
            Params = args
        };
    }
}