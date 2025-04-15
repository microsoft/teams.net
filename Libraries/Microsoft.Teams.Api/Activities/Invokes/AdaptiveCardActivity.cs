using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities.Invokes;

public partial class Name : StringEnum
{
    public bool IsAdaptiveCard => Value.StartsWith("adaptiveCard/");
}

/// <summary>
/// Any AdaptiveCard Activity
/// </summary>
[JsonConverter(typeof(JsonConverter))]
public class AdaptiveCardActivity(Name.AdaptiveCards name) : InvokeActivity(new(name.Value))
{
    public AdaptiveCards.ActionActivity ToAction() => (AdaptiveCards.ActionActivity)this;

    public override object ToType(Type type, IFormatProvider? provider)
    {
        if (type == typeof(AdaptiveCards.ActionActivity)) return ToAction();
        return this;
    }

    public new class JsonConverter : JsonConverter<AdaptiveCardActivity>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return base.CanConvert(typeToConvert);
        }

        public override AdaptiveCardActivity? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var element = JsonSerializer.Deserialize<JsonElement>(ref reader, options);

            if (!element.TryGetProperty("name", out JsonElement property))
            {
                throw new JsonException("invoke activity must have a 'name' property");
            }

            var name = property.Deserialize<string>(options);

            if (name == null)
            {
                throw new JsonException("failed to deserialize invoke activity 'name' property");
            }

            return name switch
            {
                "adaptiveCard/action" => JsonSerializer.Deserialize<AdaptiveCards.ActionActivity>(element.ToString(), options),
                _ => JsonSerializer.Deserialize<AdaptiveCardActivity>(element.ToString(), options)
            };
        }

        public override void Write(Utf8JsonWriter writer, AdaptiveCardActivity value, JsonSerializerOptions options)
        {
            if (value is AdaptiveCards.ActionActivity action)
            {
                JsonSerializer.Serialize(writer, action, options);
                return;
            }

            JsonSerializer.Serialize(writer, value, options);
        }
    }
}