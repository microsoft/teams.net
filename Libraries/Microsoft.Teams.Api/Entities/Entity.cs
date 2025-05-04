using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Entities;

[JsonConverter(typeof(JsonConverter))]
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
            var element = JsonSerializer.Deserialize<JsonElement>(ref reader, options);

            if (!element.TryGetProperty("type", out JsonElement property))
            {
                throw new JsonException("entity must have a 'type' property");
            }

            var type = property.Deserialize<string>(options);

            if (type is null)
            {
                throw new JsonException("failed to deserialize entity 'type' property");
            }

            return type switch
            {
                "clientInfo" => JsonSerializer.Deserialize<ClientInfoEntity>(element.ToString(), options),
                "mention" => JsonSerializer.Deserialize<MentionEntity>(element.ToString(), options),
                "message" or "https://schema.org/Message" => JsonSerializer.Deserialize<IMessageEntity>(element.ToString(), options),
                "streaminfo" => JsonSerializer.Deserialize<StreamInfoEntity>(element.ToString(), options),
                _ => JsonSerializer.Deserialize<Entity>(element.ToString(), options)
            };
        }

        public override void Write(Utf8JsonWriter writer, IEntity value, JsonSerializerOptions options)
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

            if (value is StreamInfoEntity streamInfo)
            {
                JsonSerializer.Serialize(writer, streamInfo, options);
                return;
            }

            JsonSerializer.Serialize(writer, value, options);
        }
    }
}

[JsonConverter(typeof(JsonConverter))]
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

    public class JsonConverter : JsonConverter<Entity>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return base.CanConvert(typeToConvert);
        }

        public override Entity? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var element = JsonSerializer.Deserialize<JsonElement>(ref reader, options);

            if (!element.TryGetProperty("type", out JsonElement property))
            {
                throw new JsonException("entity must have a 'type' property");
            }

            var type = property.Deserialize<string>(options);

            if (type is null)
            {
                throw new JsonException("failed to deserialize entity 'type' property");
            }

            return type switch
            {
                "clientInfo" => JsonSerializer.Deserialize<ClientInfoEntity>(element.ToString(), options),
                "mention" => JsonSerializer.Deserialize<MentionEntity>(element.ToString(), options),
                "message" or "https://schema.org/Message" => (Entity?)JsonSerializer.Deserialize<IMessageEntity>(element.ToString(), options),
                "streaminfo" => JsonSerializer.Deserialize<StreamInfoEntity>(element.ToString(), options),
                _ => JsonSerializer.Deserialize<Entity>(element.ToString(), options)
            };
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

            if (value is StreamInfoEntity streamInfo)
            {
                JsonSerializer.Serialize(writer, streamInfo, options);
                return;
            }

            JsonSerializer.Serialize(writer, value, options);
        }
    }
}