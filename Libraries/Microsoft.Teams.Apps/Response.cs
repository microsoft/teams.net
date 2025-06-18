using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Apps;

/// <summary>
/// Represents a response returned by a bot when it receives an activity.
/// </summary>
public class Response
{
    /// <summary>
    /// The HTTP status code of the response.
    /// </summary>
    [JsonPropertyName("status")]
    [JsonPropertyOrder(0)]
    public HttpStatusCode Status { get; set; }

    /// <summary>
    /// Optional. The body of the response.
    /// </summary>
    [JsonPropertyName("body")]
    [JsonPropertyOrder(1)]
    public object? Body { get; set; }

    public Response(HttpStatusCode status, object? body = null)
    {
        Status = status;
        Body = body;
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }
}

/// <summary>
/// Represents a response returned by a bot when it receives an activity.
/// </summary>
public class Response<T> : Response where T : notnull
{
    [JsonPropertyName("body")]
    [JsonPropertyOrder(1)]
    public new T Body { get; set; }

    public Response(HttpStatusCode status, T body) : base(status, body)
    {
        Body = body;
    }
}