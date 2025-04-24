using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.AI.Messages;

public class ModelMessage(object? content) : ModelMessage<object?>(content)
{
    public static ModelMessage<string> Text(string content) => new(content);
    public static ModelMessage<IEnumerable<IContent>> Text(IEnumerable<IContent> content) => new(content);
    public static ModelMessage<Stream> Media(Stream content) => new(content);
}

public class ModelMessage<T> : IMessage
{
    [JsonPropertyName("role")]
    [JsonPropertyOrder(0)]
    public Role Role => Role.Model;

    [JsonPropertyName("content")]
    [JsonPropertyOrder(1)]
    public T Content { get; set; }

    [JsonPropertyName("function_calls")]
    [JsonPropertyOrder(2)]
    public IList<FunctionCall>? FunctionCalls { get; set; }

    [JsonIgnore]
    public bool HasFunctionCalls => FunctionCalls != null && FunctionCalls.Count > 0;

    [JsonConstructor]
    public ModelMessage(T content, IList<FunctionCall>? functionCalls = null)
    {
        Content = content;
        FunctionCalls = functionCalls;
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
/// represents a models request to
/// invoke a function
/// </summary>
public class FunctionCall
{
    [JsonPropertyName("id")]
    [JsonPropertyOrder(0)]
    public required string Id { get; set; }

    [JsonPropertyName("name")]
    [JsonPropertyOrder(1)]
    public required string Name { get; set; }

    [JsonPropertyName("arguments")]
    [JsonPropertyOrder(2)]
    public string? Arguments { get; set; }

    public IDictionary<string, object?>? Parse()
    {
        return JsonSerializer.Deserialize<Dictionary<string, object?>>(Arguments ?? "{}");
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