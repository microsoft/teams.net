using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using Json.Schema;

using Microsoft.Teams.AI.Annotations;
using Microsoft.Teams.AI.Messages;
using Microsoft.Teams.Common.Json;

namespace Microsoft.Teams.AI;

/// <summary>
/// defines a block of code that
/// can be called by a model
/// </summary>
[JsonConverter(typeof(TrueTypeJsonConverter<IFunction>))]
public interface IFunction
{
    /// <summary>
    /// the unique name
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// a description of what the function
    /// should be used for
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// the Json Schema representing what
    /// parameters the function accepts
    /// </summary>
    public JsonSchema? Parameters { get; }
}

/// <summary>
/// defines a block of code that
/// can be called by a model
/// </summary>
public class Function : IFunction
{
    [JsonPropertyName("name")]
    [JsonPropertyOrder(0)]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    [JsonPropertyOrder(1)]
    public string? Description { get; set; }

    [JsonPropertyName("parameters")]
    [JsonPropertyOrder(2)]
    public JsonSchema? Parameters { get; set; }

    [JsonIgnore]
    public Delegate Handler { get; set; }

    public Function(string name, string? description, Delegate handler)
    {
        Name = name;
        Description = description;
        Handler = handler;
    }

    public Function(string name, string? description, JsonSchema? parameters, Delegate handler)
    {
        Name = name;
        Description = description;
        Parameters = parameters;
        Handler = handler;
    }

    internal async Task<object?> Invoke(FunctionCall call)
    {
        if (call.Arguments is not null && Parameters is not null)
        {
            var valid = Parameters.Evaluate(JsonNode.Parse(call.Arguments), new() { EvaluateAs = SpecVersion.DraftNext });

            if (!valid.IsValid)
            {
                Console.WriteLine(JsonSerializer.Serialize(valid));
                throw new ArgumentException(
                    string.Join("\n", valid.Errors?.Select(e => $"{e.Key} => {e.Value}") ?? [])
                );
            }
        }

        var args = call.Parse() ?? new Dictionary<string, object?>();
        var method = Handler.GetMethodInfo();
        var parameters = method.GetParameters().Select(param =>
        {
            var name = param.GetCustomAttribute<ParamAttribute>()?.Name ?? param.Name ?? param.Position.ToString();
            args.TryGetValue(name, out var value);

            if (value is JsonElement element)
            {
                return element.Deserialize(param.ParameterType);
            }

            return value;
        }).ToArray();

        var res = method.Invoke(Handler.Target, parameters);

        if (res is Task task)
        {
            await task.ConfigureAwait(false);
            res = ((dynamic)task).Result;
        }

        return res;
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions()
        {
            WriteIndented = true
        });
    }
}