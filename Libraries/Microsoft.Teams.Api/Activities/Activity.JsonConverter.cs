using System.Text.Json;
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
            var element = JsonSerializer.Deserialize<JsonElement>(ref reader, options);

            if (!element.TryGetProperty("type", out JsonElement property))
            {
                throw new JsonException("activity must have a 'type' property");
            }

            var type = property.Deserialize<string>(options);

            if (type is null)
            {
                throw new JsonException("failed to deserialize activity 'type' property");
            }

            return type switch
            {
                "typing" => JsonSerializer.Deserialize<TypingActivity>(element.ToString(), options),
                "message" => JsonSerializer.Deserialize<MessageActivity>(element.ToString(), options),
                "messageUpdate" => JsonSerializer.Deserialize<MessageUpdateActivity>(element.ToString(), options),
                "messageDelete" => JsonSerializer.Deserialize<MessageDeleteActivity>(element.ToString(), options),
                "messageReaction" => JsonSerializer.Deserialize<MessageReactionActivity>(element.ToString(), options),
                "conversationUpdate" => JsonSerializer.Deserialize<ConversationUpdateActivity>(element.ToString(), options),
                "endOfConversation" => JsonSerializer.Deserialize<EndOfConversationActivity>(element.ToString(), options),
                "installationUpdate" => JsonSerializer.Deserialize<InstallUpdateActivity>(element.ToString(), options),
                "command" => JsonSerializer.Deserialize<CommandActivity>(element.ToString(), options),
                "commandResult" => JsonSerializer.Deserialize<CommandResultActivity>(element.ToString(), options),
                "event" => JsonSerializer.Deserialize<EventActivity>(element.ToString(), options),
                "invoke" => JsonSerializer.Deserialize<InvokeActivity>(element.ToString(), options),
                _ => JsonSerializer.Deserialize<Activity>(element.ToString(), options)
            };
        }

        public override void Write(Utf8JsonWriter writer, IActivity value, JsonSerializerOptions options)
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

            if (value is Activity activity)
            {
                JsonSerializer.Serialize(writer, activity, options);
                return;
            }

            JsonSerializer.Serialize(writer, value, options);
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
            var element = JsonSerializer.Deserialize<JsonElement>(ref reader, options);

            if (!element.TryGetProperty("type", out JsonElement property))
            {
                throw new JsonException("activity must have a 'type' property");
            }

            var type = property.Deserialize<string>(options);

            if (type is null)
            {
                throw new JsonException("failed to deserialize activity 'type' property");
            }

            return type switch
            {
                "typing" => JsonSerializer.Deserialize<TypingActivity>(element.ToString(), options),
                "message" => JsonSerializer.Deserialize<MessageActivity>(element.ToString(), options),
                "messageUpdate" => JsonSerializer.Deserialize<MessageUpdateActivity>(element.ToString(), options),
                "messageDelete" => JsonSerializer.Deserialize<MessageDeleteActivity>(element.ToString(), options),
                "messageReaction" => JsonSerializer.Deserialize<MessageReactionActivity>(element.ToString(), options),
                "conversationUpdate" => JsonSerializer.Deserialize<ConversationUpdateActivity>(element.ToString(), options),
                "endOfConversation" => JsonSerializer.Deserialize<EndOfConversationActivity>(element.ToString(), options),
                "installationUpdate" => JsonSerializer.Deserialize<InstallUpdateActivity>(element.ToString(), options),
                "command" => JsonSerializer.Deserialize<CommandActivity>(element.ToString(), options),
                "commandResult" => JsonSerializer.Deserialize<CommandResultActivity>(element.ToString(), options),
                "event" => JsonSerializer.Deserialize<EventActivity>(element.ToString(), options),
                "invoke" => JsonSerializer.Deserialize<InvokeActivity>(element.ToString(), options),
                _ => JsonSerializer.Deserialize<Activity>(element.ToString(), options)
            };
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

            JsonSerializer.Serialize(writer, value, options);
        }
    }
}