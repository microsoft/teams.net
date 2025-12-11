// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Bot.Core.Schema;

/// <summary>
/// JSON source generator context for Core activity types.
/// This enables AOT-compatible and reflection-free JSON serialization.
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(CoreActivity))]
[JsonSerializable(typeof(ChannelData))]
[JsonSerializable(typeof(Conversation))]
[JsonSerializable(typeof(ConversationAccount))]
[JsonSerializable(typeof(ExtendedPropertiesDictionary))]
[JsonSerializable(typeof(System.Text.Json.JsonElement))]
[JsonSerializable(typeof(System.Int32))]
[JsonSerializable(typeof(System.Boolean))]
[JsonSerializable(typeof(System.Int64))]
[JsonSerializable(typeof(System.Double))]
public partial class CoreActivityJsonContext : JsonSerializerContext
{
}
