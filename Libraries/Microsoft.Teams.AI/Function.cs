using System.Text.Json.Serialization;

using Json.Schema;

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
public class Function : Function<object>
{
    public Function(string name, string? description, Func<object?, Task<object?>> handler) : base(name, description, handler)
    {
    }

    public Function(string name, string? description, JsonSchema parameters, Func<object?, Task<object?>> handler) : base(name, description, parameters, handler)
    {
    }
}

/// <summary>
/// defines a block of code that
/// can be called by a model
/// </summary>
public class Function<T> : IFunction
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

    internal Func<T, Task<object?>> Handler { get; set; }

    public Function(string name, string? description, Func<T, Task<object?>> handler)
    {
        Name = name;
        Description = description;
        Handler = handler;
    }

    public Function(string name, string? description, JsonSchema parameters, Func<T, Task<object?>> handler)
    {
        Name = name;
        Description = description;
        Parameters = parameters;
        Handler = handler;
    }

    internal Task<object?> Invoke(T args) => Handler(args);
    internal Task<object?> Invoke(object? args) => Handler((T?)args ?? throw new InvalidDataException());
}