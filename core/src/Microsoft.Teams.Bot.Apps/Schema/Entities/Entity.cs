// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Teams.Bot.Core.Schema;

namespace Microsoft.Teams.Bot.Apps.Schema;


/// <summary>
/// List of Entity objects.
/// </summary>
[JsonConverter(typeof(EntityListJsonConverter))]
public class EntityList : List<Entity>
{
    /// <summary>
    /// Converts the Entities collection to a JsonArray.
    /// </summary>
    /// <returns></returns>
    public JsonArray? ToJsonArray()
    {
        JsonArray jsonArray = [];
        foreach (Entity entity in this)
        {
            JsonObject jsonObject = new()
            {
                ["type"] = entity.Type
            };

            // For entities with direct properties (e.g., CitationEntity.Citation),
            // serialize via JsonSerializer to capture all [JsonPropertyName] properties.
            if (entity is CitationEntity citationEntity)
            {
                var serialized = JsonSerializer.SerializeToNode(citationEntity);
                if (serialized is JsonObject citationJson)
                {
                    jsonArray.Add(citationJson);
                    continue;
                }
            }

            foreach (KeyValuePair<string, object?> property in entity.Properties)
            {
                jsonObject[property.Key] = property.Value as JsonNode ?? JsonValue.Create(property.Value);
            }
            jsonArray.Add(jsonObject);
        }
        return jsonArray;
    }

    /// <summary>
    /// Parses a JsonArray into an Entities collection.
    /// </summary>
    /// <param name="jsonArray"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static EntityList FromJsonArray(JsonArray? jsonArray, JsonSerializerOptions? options = null)
    {
        if (jsonArray == null)
        {
            return [];
        }
        EntityList entities = [];
        foreach (JsonNode? item in jsonArray)
        {
            if (item is JsonObject jsonObject
                && jsonObject.TryGetPropertyValue("type", out JsonNode? typeNode)
                && typeNode is JsonValue typeValue
                && typeValue.GetValue<string>() is string typeString)
            {

                // TODO: Should be able to support unknown types (PA uses BotMessageMetadata).
                // TODO: Investigate if there is any way for Parent to avoid
                // Knowing the children.
                // Maybe a registry pattern, or Converters?
                Entity? entity = typeString switch
                {
                    "clientInfo" => item.Deserialize<ClientInfoEntity>(options),
                    "mention" => item.Deserialize<MentionEntity>(options),
                    "message" or "https://schema.org/Message" => DeserializeMessageEntity(item, options),
                    "ProductInfo" => item.Deserialize<ProductInfoEntity>(options),
                    "streaminfo" => item.Deserialize<StreamInfoEntity>(options),
                    _ => null
                };
                if (entity != null)
                    entities.Add(entity);
            }
        }
        return entities;
    }

    /// <summary>
    /// Deserializes a message entity by checking the @type property to determine the specific type.
    /// </summary>
    /// <param name="item">The JSON node to deserialize.</param>
    /// <param name="options">The JSON serializer options.</param>
    /// <returns>The deserialized entity, or null if deserialization fails.</returns>
    private static OMessageEntity? DeserializeMessageEntity(JsonNode item, JsonSerializerOptions? options)
    {
        if (item is JsonObject jsonObject
            && jsonObject.TryGetPropertyValue("@type", out JsonNode? oTypeNode)
            && oTypeNode is JsonValue oTypeValue
            && oTypeValue.GetValue<string>() is string oType)
        {
            return oType switch
            {
                "Message" => item.Deserialize<CitationEntity>(options),
                "CreativeWork" => item.Deserialize<SensitiveUsageEntity>(options),
                _ => item.Deserialize<OMessageEntity>(options)
            };
        }

        return item.Deserialize<OMessageEntity>(options);
    }
}

/// <summary>
/// Entity base class.
/// </summary>
/// <remarks>
/// Initializes a new instance of the Entity class with the specified type.
/// </remarks>
/// <param name="type">The type of the entity. Cannot be null.</param>
public class Entity(string type)
{
    /// <summary>
    /// Gets or sets the type identifier for the object represented by this instance.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = type;

    /// <summary>
    /// Gets or sets the OData type identifier for the object represented by this instance.
    /// </summary>
    [JsonPropertyName("@type")] public string? OType { get; set; }

    /// <summary>
    /// Gets or sets the OData context for the object represented by this instance.
    /// </summary>
    [JsonPropertyName("@context")] public string? OContext { get; set; }
    /// <summary>
    /// Extended properties dictionary.
    /// </summary>
    [JsonExtensionData] public ExtendedPropertiesDictionary Properties { get; set; } = [];

}

/// <summary>
/// JSON converter for EntityList.
/// </summary>
public class EntityListJsonConverter : JsonConverter<EntityList>
{
    /// <summary>
    /// Reads and converts the JSON to EntityList.
    /// </summary>
    public override EntityList? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        JsonArray? jsonArray = JsonSerializer.Deserialize<JsonArray>(ref reader, options);
        return EntityList.FromJsonArray(jsonArray, options);
    }

    /// <summary>
    /// Writes the EntityList as JSON.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, EntityList value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(value);
        JsonArray? jsonArray = value.ToJsonArray();
        JsonSerializer.Serialize(writer, jsonArray, options);
    }
}

