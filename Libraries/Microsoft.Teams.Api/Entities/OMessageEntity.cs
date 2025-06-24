// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Entities;

[JsonConverter(typeof(OMessageJsonConverter))]
public class OMessageEntity : Entity, IMessageEntity
{
    [JsonPropertyName("additionalType")]
    [JsonPropertyOrder(10)]
    public IList<string>? AdditionalType { get; set; }

    public OMessageEntity() : base("https://schema.org/Message")
    {
        OType = "Message";
        OContext = "https://schema.org";
    }

    public class OMessageJsonConverter : JsonConverter<OMessageEntity>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return base.CanConvert(typeToConvert);
        }

        public override OMessageEntity? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var element = JsonSerializer.Deserialize<JsonElement>(ref reader, options);

            if (!element.TryGetProperty("@type", out JsonElement property))
            {
                throw new JsonException("'https://schema.org/Message' entity must have a '@type' property");
            }

            var oType = property.Deserialize<string>(options);

            if (oType is null)
            {
                throw new JsonException("failed to deserialize 'https://schema.org/Message' entity '@type' property");
            }

            return oType switch
            {
                "Claim" => JsonSerializer.Deserialize<CitationEntity>(element.ToString(), options),
                "CreativeWork" => JsonSerializer.Deserialize<SensitiveUsageEntity>(element.ToString(), options),
                _ => JsonSerializer.Deserialize<OMessageEntity>(element.ToString(), options)
            };
        }

        public override void Write(Utf8JsonWriter writer, OMessageEntity value, JsonSerializerOptions options)
        {
            if (value is CitationEntity citation)
            {
                JsonSerializer.Serialize(writer, citation, options);
                return;
            }

            if (value is SensitiveUsageEntity sensitiveUsage)
            {
                JsonSerializer.Serialize(writer, sensitiveUsage, options);
                return;
            }

            JsonSerializer.Serialize(writer, value, options);
        }
    }
}