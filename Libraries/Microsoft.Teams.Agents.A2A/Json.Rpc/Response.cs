using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Agents.A2A.Json.Rpc;

public class Response
{
    [JsonPropertyName("jsonrpc")]
    [JsonPropertyOrder(0)]
    public string Version => "2.0";

    [JsonPropertyName("id")]
    [JsonPropertyOrder(1)]
    public string? Id { get; set; }

    [JsonPropertyName("result")]
    [JsonPropertyOrder(2)]
    public object? Result { get; set; }

    [JsonPropertyName("error")]
    [JsonPropertyOrder(3)]
    public Error? Error { get; set; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }

    public Response Ok(object? result = null)
    {
        Result = result;
        return this;
    }

    public Response Err(Error? error = null)
    {
        Error = error;
        return this;
    }
}