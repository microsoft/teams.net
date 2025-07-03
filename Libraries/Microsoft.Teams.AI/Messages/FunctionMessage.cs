// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.AI.Messages;

public class FunctionMessage : IMessage
{
    [JsonPropertyName("role")]
    [JsonPropertyOrder(0)]
    public Role Role => Role.Function;

    [JsonPropertyName("content")]
    [JsonPropertyOrder(1)]
    public string? Content { get; set; }

    [JsonPropertyName("function_id")]
    [JsonPropertyOrder(2)]
    public required string FunctionId { get; set; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }
}