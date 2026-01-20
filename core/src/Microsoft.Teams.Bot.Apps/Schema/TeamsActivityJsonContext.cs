// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Teams.Bot.Apps.Schema.Entities;
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
[JsonSerializable(typeof(Entity))]
[JsonSerializable(typeof(EntityList))]
[JsonSerializable(typeof(MentionEntity))]
[JsonSerializable(typeof(ClientInfoEntity))]
[JsonSerializable(typeof(TeamsChannelData))]
[JsonSerializable(typeof(ConversationAccount))]
[JsonSerializable(typeof(TeamsConversationAccount))]
[JsonSerializable(typeof(TeamsConversation))]
[JsonSerializable(typeof(ExtendedPropertiesDictionary))]
[JsonSerializable(typeof(System.Text.Json.JsonElement))]
[JsonSerializable(typeof(System.Int32))]
[JsonSerializable(typeof(System.Boolean))]
[JsonSerializable(typeof(System.Int64))]
[JsonSerializable(typeof(System.Double))]
public partial class TeamsActivityJsonContext : JsonSerializerContext
{
}
