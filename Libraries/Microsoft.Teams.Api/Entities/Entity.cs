// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using Microsoft.Teams.Common.Json;

namespace Microsoft.Teams.Api.Entities;

[JsonConverter(typeof(EntityJsonConverter))]
public interface IEntity
{
    [JsonPropertyName("type")]
    [JsonPropertyOrder(0)]
    public string Type { get; set; }

    [JsonPropertyName("@type")]
    [JsonPropertyOrder(1)]
    public string? OType { get; set; }

    [JsonPropertyName("@context")]
    [JsonPropertyOrder(2)]
    public string? OContext { get; set; }

    [JsonExtensionData]
    public IDictionary<string, object?> Properties { get; set; }

    public class JsonConverter : JsonConverter<IEntity>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return base.CanConvert(typeToConvert);
        }

        public override IEntity? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<Entity>(ref reader, options);
        }

        public override void Write(Utf8JsonWriter writer, IEntity value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }
}

[JsonConverter(typeof(EntityJsonConverter))]
public class Entity : IEntity
{
    [JsonPropertyName("type")]
    [JsonPropertyOrder(0)]
    public string Type { get; set; }

    [JsonPropertyName("@type")]
    [JsonPropertyOrder(1)]
    public string? OType { get; set; }

    [JsonPropertyName("@context")]
    [JsonPropertyOrder(2)]
    public string? OContext { get; set; }

    [JsonExtensionData]
    public IDictionary<string, object?> Properties { get; set; } = new Dictionary<string, object?>();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [JsonConstructor]
    public Entity(string type)
    {
        Type = type;
    }

    public Entity(string type, string? otype)
    {
        Type = type;
        OType = otype;
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, JsonOptions);
    }

    public class JsonConverter : JsonConverter<Entity>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return base.CanConvert(typeToConvert);
        }

        public override Entity? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var element = JsonSerializer.Deserialize<JsonObject>(ref reader, options) ?? throw new Exception("expected json object");

            if (!element.TryGetPropertyValue("type", out var typeNode))
            {
                throw new JsonException("entity must have a 'type' property");
            }

            var type = typeNode.Deserialize<string>(options);

            if (type is null)
            {
                throw new JsonException("failed to deserialize entity 'type' property");
            }

            Entity? entity = type switch
            {
                "clientInfo" => element.Deserialize<ClientInfoEntity>(options),
                "mention" => element.Deserialize<MentionEntity>(options),
                "message" or "https://schema.org/Message" => (Entity?)element.Deserialize<IMessageEntity>(options),
                "ProductInfo" => element.Deserialize<ProductInfoEntity>(options),
                "streaminfo" => element.Deserialize<StreamInfoEntity>(options),
                _ => null
            };

            if (entity is null)
            {
                entity = new(type);
                entity.Properties = entity.FromJsonObject(element, options);
            }

            return entity;
        }

        public override void Write(Utf8JsonWriter writer, Entity value, JsonSerializerOptions options)
        {
            if (value is ClientInfoEntity clientInfo)
            {
                JsonSerializer.Serialize(writer, clientInfo, options);
                return;
            }

            if (value is MentionEntity mention)
            {
                JsonSerializer.Serialize(writer, mention, options);
                return;
            }

            if (value is IMessageEntity message)
            {
                JsonSerializer.Serialize(writer, message, options);
                return;
            }

            if (value is ProductInfoEntity productInfo)
            {
                JsonSerializer.Serialize(writer, productInfo, options);
                return;
            }

            if (value is StreamInfoEntity streamInfo)
            {
                JsonSerializer.Serialize(writer, streamInfo, options);
                return;
            }

            JsonSerializer.Serialize(writer, value.ToJsonObject(options), options);
        }
    }
}

public class EntityJsonConverter : JsonConverterFactory
{
    public override bool CanConvert(Type type)
    {
        return typeof(IEntity).IsAssignableFrom(type);
    }

    public override JsonConverter? CreateConverter(Type type, JsonSerializerOptions options)
    {
        return type == typeof(Entity) ? new Entity.JsonConverter() : new IEntity.JsonConverter();
    }
}