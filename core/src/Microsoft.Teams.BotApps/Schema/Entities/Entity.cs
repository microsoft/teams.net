// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Bot.Core.Schema;

namespace Microsoft.Teams.BotApps.Schema.Entities;


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

                Entity? entity = typeString switch
                {
                    "clientInfo" => item.Deserialize<ClientInfoEntity>(options),
                    "mention" => item.Deserialize<MentionEntity>(options),
                    //"message" or "https://schema.org/Message" => (Entity?)item.Deserialize<IMessageEntity>(options),
                    "ProductInfo" => item.Deserialize<ProductInfoEntity>(options),
                    "streaminfo" => item.Deserialize<StreamInfoEntity>(options),
                    _ => null
                };
                //foreach (var property in jsonObject)
                //{
                //    if (property.Key != "type")
                //    {
                //        entity?.Properties[property.Key] = property.Value!;
                //    }
                //}
                if (entity != null)
                    entities.Add(entity);
            }
        }
        return entities;
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
#pragma warning disable CA2227 // Collection properties should be read only
    [JsonExtensionData] public ExtendedPropertiesDictionary Properties { get; set; } = [];
#pragma warning restore CA2227 // Collection properties should be read only

    /// <summary>
    /// Adds properties to the Properties dictionary.
    /// </summary>
    protected virtual void ToProperties()
    {
        throw new NotImplementedException();
    }

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

