using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Entities;

[JsonConverter(typeof(IMessageJsonConverter))]
public interface IMessageEntity : IEntity
{
    [JsonPropertyName("additionalType")]
    [JsonPropertyOrder(10)]
    public IList<string>? AdditionalType { get; set; }

    public class IMessageJsonConverter : JsonConverter<IMessageEntity>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return base.CanConvert(typeToConvert);
        }

        public override IMessageEntity? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
                "message" => JsonSerializer.Deserialize<MessageEntity>(element.ToString(), options),
                "https://schema.org/Message" => JsonSerializer.Deserialize<OMessageEntity>(element.ToString(), options),
                _ => throw new JsonException($"entity type '{type}' is not supported")
            };
        }

        public override void Write(Utf8JsonWriter writer, IMessageEntity value, JsonSerializerOptions options)
        {
            if (value is MessageEntity message)
            {
                JsonSerializer.Serialize(writer, message, options);
                return;
            }

            if (value is OMessageEntity oMessage)
            {
                JsonSerializer.Serialize(writer, oMessage, options);
                return;
            }

            JsonSerializer.Serialize(writer, value, options);
        }
    }
}

public class MessageEntity : Entity, IMessageEntity
{
    [JsonPropertyName("additionalType")]
    [JsonPropertyOrder(10)]
    public IList<string>? AdditionalType { get; set; }

    public MessageEntity() : base("message") { }
}