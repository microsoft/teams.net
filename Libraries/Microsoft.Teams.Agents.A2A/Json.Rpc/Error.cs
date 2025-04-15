using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Agents.A2A.Json.Rpc;

public class Error
{
    [JsonPropertyName("code")]
    [JsonPropertyOrder(0)]
    public required int Code { get; set; }

    [JsonPropertyName("message")]
    [JsonPropertyOrder(1)]
    public required string Message { get; set; }

    [JsonPropertyName("data")]
    [JsonPropertyOrder(2)]
    public object? Data { get; set; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }
}

public enum ErrorCode : int
{
    ParseError = -32700,
    InvalidRequest = -32600,
    MethodNotFound = -32601,
    InvalidParams = -32602,
    InternalError = -32603
}

public static class ErrorCodeExtensions
{
    public static string ToString(this ErrorCode code)
    {
        return code switch
        {
            ErrorCode.ParseError => "Parse Error",
            ErrorCode.InvalidRequest => "Invalid Request",
            ErrorCode.MethodNotFound => "Method Not Found",
            ErrorCode.InvalidParams => "Invalid Params",
            ErrorCode.InternalError => "Internal Error",
            _ => "Server Error"
        };
    }
}