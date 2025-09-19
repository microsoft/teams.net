// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.TaskModules;

/// <summary>
/// Task Types
/// </summary>
[JsonConverter(typeof(JsonConverter<TaskType>))]
public partial class TaskType(string value) : StringEnum(value)
{
}

/// <summary>
/// Base class for Task Module responses
/// </summary>
[JsonConverter(typeof(TaskJsonConverter))]
public abstract class Task(TaskType type)
{
    /// <summary>
    /// Choice of action options when responding to the
    /// task/submit message. Possible values include: 'message', 'continue'
    /// </summary>
    [JsonPropertyName("type")]
    [JsonPropertyOrder(0)]
    public TaskType Type { get; set; } = type;
}

/// <summary>
/// JSON converter for polymorphic Task serialization
/// </summary>
public class TaskJsonConverter : JsonConverter<Task>
{
    public override Task? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (root.TryGetProperty("type", out var typeElement))
        {
            var typeValue = typeElement.GetString();
            return typeValue switch
            {
                "continue" => JsonSerializer.Deserialize<ContinueTask>(root.GetRawText(), options),
                "message" => JsonSerializer.Deserialize<MessageTask>(root.GetRawText(), options),
                _ => throw new JsonException($"Unknown task type: {typeValue}")
            };
        }

        throw new JsonException("Task type property not found");
    }

    public override void Write(Utf8JsonWriter writer, Task value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}