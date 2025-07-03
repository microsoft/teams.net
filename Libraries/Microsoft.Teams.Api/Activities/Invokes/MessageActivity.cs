// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities.Invokes;

public partial class Name : StringEnum
{
    public bool IsMessage => Value.StartsWith("message/");
}

/// <summary>
/// Any Message Activity
/// </summary>
[JsonConverter(typeof(JsonConverter))]
public abstract class MessageActivity(Name.Messages name) : InvokeActivity(new(name.Value))
{
    public Messages.SubmitActionActivity ToSubmitAction() => (Messages.SubmitActionActivity)this;

    public override object ToType(Type type, IFormatProvider? provider)
    {
        if (type == typeof(Messages.SubmitActionActivity)) return ToSubmitAction();
        return this;
    }

    public new class JsonConverter : JsonConverter<MessageActivity>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return base.CanConvert(typeToConvert);
        }

        public override MessageActivity? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var element = JsonSerializer.Deserialize<JsonElement>(ref reader, options);

            if (!element.TryGetProperty("name", out JsonElement property))
            {
                throw new JsonException("invoke activity must have a 'name' property");
            }

            var name = property.Deserialize<string>(options);

            if (name is null)
            {
                throw new JsonException("failed to deserialize invoke activity 'name' property");
            }

            return name switch
            {
                "message/submitAction" => JsonSerializer.Deserialize<Messages.SubmitActionActivity>(element.ToString(), options),
                _ => JsonSerializer.Deserialize<MessageActivity>(element.ToString(), options)
            };
        }

        public override void Write(Utf8JsonWriter writer, MessageActivity value, JsonSerializerOptions options)
        {
            if (value is Messages.SubmitActionActivity submitAction)
            {
                JsonSerializer.Serialize(writer, submitAction, options);
                return;
            }

            JsonSerializer.Serialize(writer, value, options);
        }
    }
}