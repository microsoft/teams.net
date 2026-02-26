// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using NJsonSchema;

namespace Microsoft.Teams.AI;

/// <summary>
/// NJsonSchema-based implementation of IJsonSchema.
/// </summary>
public class JsonSchemaWrapper : IJsonSchema
{
    private readonly JsonSchema _schema;

    private JsonSchemaWrapper(JsonSchema schema) => _schema = schema;

    /// <summary>
    /// Creates a schema from a .NET type using reflection.
    /// </summary>
    public static IJsonSchema FromType(Type type)
    {
        var schema = JsonSchema.FromType(type);
        return new JsonSchemaWrapper(schema);
    }

    /// <summary>
    /// Creates a schema from a JSON schema string.
    /// </summary>
    public static IJsonSchema FromJson(string json)
    {
        var schema = JsonSchema.FromJsonAsync(json).GetAwaiter().GetResult();
        return new JsonSchemaWrapper(schema);
    }

    /// <summary>
    /// Creates an object schema with the specified properties.
    /// </summary>
    public static IJsonSchema CreateObjectSchema(params (string name, IJsonSchema schema, bool required)[] properties)
    {
        var resultSchema = new JsonSchema { Type = JsonObjectType.Object };

        foreach (var (name, propSchema, required) in properties)
        {
            if (propSchema is JsonSchemaWrapper wrapper)
            {
                var property = new JsonSchemaProperty
                {
                    Type = wrapper._schema.Type,
                    Description = wrapper._schema.Description
                };
                resultSchema.Properties.Add(name, property);
            }

            if (required)
            {
                resultSchema.RequiredProperties.Add(name);
            }
        }

        return new JsonSchemaWrapper(resultSchema);
    }

    /// <summary>
    /// Creates a string schema.
    /// </summary>
    public static IJsonSchema String(string? description = null)
    {
        var schema = new JsonSchema
        {
            Type = JsonObjectType.String,
            Description = description
        };
        return new JsonSchemaWrapper(schema);
    }

    /// <inheritdoc/>
    public JsonSchemaValidationResult Validate(string json)
    {
        var errors = _schema.Validate(json);
        return new JsonSchemaValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors.Select(e => new JsonSchemaValidationError(e.Path ?? string.Empty, e.Kind.ToString())).ToList()
        };
    }

    /// <inheritdoc/>
    public string ToJson() => _schema.ToJson();
}