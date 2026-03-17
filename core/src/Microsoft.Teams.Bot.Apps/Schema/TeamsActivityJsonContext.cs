// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.Schema;

/// <summary>
/// Json source generator context for Teams activity types.
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    IncludeFields = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(CoreActivity))]
[JsonSerializable(typeof(TeamsActivity))]
[JsonSerializable(typeof(MessageActivity))]
[JsonSerializable(typeof(TypingActivity))]
[JsonSerializable(typeof(Entity))]
[JsonSerializable(typeof(EntityList))]
[JsonSerializable(typeof(MentionEntity))]
[JsonSerializable(typeof(ClientInfoEntity))]
[JsonSerializable(typeof(OMessageEntity))]
[JsonSerializable(typeof(SensitiveUsageEntity))]
[JsonSerializable(typeof(DefinedTerm))]
[JsonSerializable(typeof(ProductInfoEntity))]
[JsonSerializable(typeof(StreamInfoEntity))]
[JsonSerializable(typeof(CitationEntity))]
[JsonSerializable(typeof(CitationClaim))]
[JsonSerializable(typeof(CitationAppearanceDocument))]
[JsonSerializable(typeof(CitationImageObject))]
[JsonSerializable(typeof(CitationAppearance))]
[JsonSerializable(typeof(TeamsChannelData))]
[JsonSerializable(typeof(ConversationAccount))]
[JsonSerializable(typeof(TeamsConversationAccount))]
[JsonSerializable(typeof(TeamsConversation))]
[JsonSerializable(typeof(ExtendedPropertiesDictionary))]
[JsonSerializable(typeof(TeamsAttachment))]
[JsonSerializable(typeof(System.Text.Json.JsonElement))]
[JsonSerializable(typeof(System.Text.Json.Nodes.JsonObject))]
[JsonSerializable(typeof(System.Text.Json.Nodes.JsonNode))]
[JsonSerializable(typeof(System.Text.Json.Nodes.JsonArray))]
[JsonSerializable(typeof(System.Text.Json.Nodes.JsonValue))]
[JsonSerializable(typeof(System.Int32))]
[JsonSerializable(typeof(System.Boolean))]
[JsonSerializable(typeof(System.Int64))]
[JsonSerializable(typeof(System.Double))]
public partial class TeamsActivityJsonContext : JsonSerializerContext
{
}
