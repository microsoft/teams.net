// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.AI;

/// <summary>
/// Abstraction for JSON schema validation, decoupling from specific schema library implementations.
/// </summary>
public interface IJsonSchema
{
    /// <summary>
    /// Validates a JSON string against this schema.
    /// </summary>
    JsonSchemaValidationResult Validate(string json);

    /// <summary>
    /// Serializes this schema to a JSON string.
    /// </summary>
    string ToJson();
}

/// <summary>
/// Result of validating JSON against a schema.
/// </summary>
public class JsonSchemaValidationResult
{
    /// <summary>
    /// Whether the JSON is valid against the schema.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// List of validation errors, if any.
    /// </summary>
    public IReadOnlyList<JsonSchemaValidationError> Errors { get; init; } = [];
}

/// <summary>
/// Represents a validation error from schema validation.
/// </summary>
/// <param name="Path">The JSON path where the error occurred.</param>
/// <param name="Message">The error message.</param>
public record JsonSchemaValidationError(string Path, string Message);