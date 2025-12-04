// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Activities;

public partial interface IActivity
{
    public class JsonConverter : JsonConverter<IActivity>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return base.CanConvert(typeToConvert);
        }

        public override IActivity? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<Activity>(ref reader, options);
        }

        public override void Write(Utf8JsonWriter writer, IActivity value, JsonSerializerOptions options)
        {
            // default to the underlying class type to avoid recursive serialization
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }
}

public partial class Activity
{
    public class JsonConverter : JsonConverter<Activity>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return base.CanConvert(typeToConvert);
        }

        public override Activity? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var element = JsonSerializer.Deserialize<JsonObject>(ref reader, options) ?? throw new Exception("expected json object");

            if (!element.TryGetPropertyValue("type", out var typeNode))
            {
                throw new JsonException("activity must have a 'type' property");
            }

            var type = typeNode.Deserialize<string>(options);

            if (type is null)
            {
                throw new JsonException("failed to deserialize activity 'type' property");
            }

            Activity? activity = type switch
            {
                "typing" => element.Deserialize<TypingActivity>(options),
                "message" => element.Deserialize<MessageActivity>(options),
                "messageUpdate" => element.Deserialize<MessageUpdateActivity>(options),
                "messageDelete" => element.Deserialize<MessageDeleteActivity>(options),
                "messageReaction" => element.Deserialize<MessageReactionActivity>(options),
                "conversationUpdate" => element.Deserialize<ConversationUpdateActivity>(options),
                "endOfConversation" => element.Deserialize<EndOfConversationActivity>(options),
                "installationUpdate" => element.Deserialize<InstallUpdateActivity>(options),
                "command" => element.Deserialize<CommandActivity>(options),
                "commandResult" => element.Deserialize<CommandResultActivity>(options),
                "event" => element.Deserialize<EventActivity>(options),
                "invoke" => element.Deserialize<InvokeActivity>(options),
                _ => null
            };

            if (activity is null)
            {
                activity = new(type);

                // TODO: Review
                activity.Properties = activity.FromJsonObject(element, options);
            }

            return activity;
        }

        public override void Write(Utf8JsonWriter writer, Activity value, JsonSerializerOptions options)
        {
            if (value is TypingActivity typing)
            {
                JsonSerializer.Serialize(writer, typing, options);
                return;
            }

            if (value is MessageActivity message)
            {
                JsonSerializer.Serialize(writer, message, options);
                return;
            }

            if (value is MessageUpdateActivity messageUpdate)
            {
                JsonSerializer.Serialize(writer, messageUpdate, options);
                return;
            }

            if (value is MessageDeleteActivity messageDelete)
            {
                JsonSerializer.Serialize(writer, messageDelete, options);
                return;
            }

            if (value is MessageReactionActivity messageReaction)
            {
                JsonSerializer.Serialize(writer, messageReaction, options);
                return;
            }

            if (value is ConversationUpdateActivity conversationUpdate)
            {
                JsonSerializer.Serialize(writer, conversationUpdate, options);
                return;
            }

            if (value is EndOfConversationActivity endOfConversation)
            {
                JsonSerializer.Serialize(writer, endOfConversation, options);
                return;
            }

            if (value is CommandActivity command)
            {
                JsonSerializer.Serialize(writer, command, options);
                return;
            }

            if (value is CommandResultActivity commandResult)
            {
                JsonSerializer.Serialize(writer, commandResult, options);
                return;
            }

            if (value is EventActivity @event)
            {
                JsonSerializer.Serialize(writer, @event, options);
                return;
            }

            if (value is InvokeActivity invoke)
            {
                JsonSerializer.Serialize(writer, invoke, options);
                return;
            }

            // TODO: Review
            JsonSerializer.Serialize(writer, value.ToJsonObject(options), options);
        }
    }
}

public class ActivityJsonConverter : JsonConverterFactory
{
    public override bool CanConvert(Type type)
    {
        return typeof(IActivity).IsAssignableFrom(type);
    }

    public override JsonConverter? CreateConverter(Type type, JsonSerializerOptions options)
    {
        return type == typeof(Activity) ? new Activity.JsonConverter() : new IActivity.JsonConverter();
    }
}