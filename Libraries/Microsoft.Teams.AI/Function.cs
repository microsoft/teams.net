// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using Json.Schema;
using Json.Schema.Generation;

using Microsoft.Teams.AI.Annotations;
using Microsoft.Teams.AI.Messages;
using Microsoft.Teams.Common.Extensions;
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
        Parameters = GenerateParametersSchema(handler);
    }

    public Function(string name, string? description, JsonSchema parameters, Delegate handler)
    {
        Name = name;
        Description = description;
        Parameters = parameters;
        Handler = handler;
    }

    internal Task<object?> Invoke(FunctionCall call)
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

            // Special param type to get the arguments dictionary (IDictionary<string, object?> args)
            if (value is null && name == "args" && param.ParameterType == typeof(IDictionary<string, object?>))
            {
                value = args;
            }

            return value;
        }).ToArray();

        return method.InvokeAsync(Handler.Target, parameters);
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions()
        {
            WriteIndented = true
        });
    }

    /// <summary>
    /// Generates a JsonSchema for the parameters of a delegate handler using reflection
    /// </summary>
    private static JsonSchema? GenerateParametersSchema(Delegate handler)
    {
        var method = handler.GetMethodInfo();
        var methodParams = method.GetParameters();

        if (methodParams.Length == 0)
        {
            return null;
        }

        var parameters = methodParams.Select(p =>
        {
            var paramName = p.GetCustomAttribute<ParamAttribute>()?.Name ?? p.Name ?? p.Position.ToString();
            var schema = new JsonSchemaBuilder().FromType(p.ParameterType).Build();
            var required = !p.IsOptional;
            return (paramName, schema, required);
        });

        return new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(parameters.Select(item => (item.paramName, item.schema)).ToArray())
            .Required(parameters.Where(item => item.required).Select(item => item.paramName))
            .Build();
    }
}