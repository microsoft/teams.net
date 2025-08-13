using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Cards;

internal sealed class CardElementJsonConverter : JsonConverter<CardElement>
{
    public override CardElement? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => throw new NotSupportedException("Deserializing CardElement is not supported by this converter.");

    public override void Write(Utf8JsonWriter writer, CardElement value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
    }
}

internal sealed class ActionJsonConverter : JsonConverter<Action>
{
    public override Action? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => throw new NotSupportedException("Deserializing Action is not supported by this converter.");

    public override void Write(Utf8JsonWriter writer, Action value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
    }
}

internal sealed class ContainerLayoutJsonConverter : JsonConverter<ContainerLayout>
{
    public override ContainerLayout? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => throw new NotSupportedException("Deserializing ContainerLayout is not supported by this converter.");

    public override void Write(Utf8JsonWriter writer, ContainerLayout value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
    }
}