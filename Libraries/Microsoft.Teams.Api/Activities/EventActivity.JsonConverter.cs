using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Activities;

public partial class EventActivity
{
    public new class JsonConverter : JsonConverter<EventActivity>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return base.CanConvert(typeToConvert);
        }

        public override EventActivity? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var element = JsonSerializer.Deserialize<JsonElement>(ref reader, options);

            if (!element.TryGetProperty("name", out JsonElement property))
            {
                throw new JsonException("event activity must have a 'name' property");
            }

            var name = property.Deserialize<string>(options);

            if (name is null)
            {
                throw new JsonException("failed to deserialize event activity 'name' property");
            }

            return name switch
            {
                "application/vnd.microsoft.meetingEnd" => JsonSerializer.Deserialize<Events.MeetingEndActivity>(element.ToString(), options),
                "application/vnd.microsoft.meetingStart" => JsonSerializer.Deserialize<Events.MeetingStartActivity>(element.ToString(), options),
                "application/vnd.microsoft.meetingParticipantJoin" => JsonSerializer.Deserialize<Events.MeetingParticipantJoinActivity>(element.ToString(), options),
                "application/vnd.microsoft.meetingParticipantLeave" => JsonSerializer.Deserialize<Events.MeetingParticipantLeaveActivity>(element.ToString(), options),
                "application/vnd.microsoft.readReceipt" => JsonSerializer.Deserialize<Events.ReadReceiptActivity>(element.ToString(), options),
                _ => JsonSerializer.Deserialize<EventActivity>(element.ToString(), options)
            };
        }

        public override void Write(Utf8JsonWriter writer, EventActivity value, JsonSerializerOptions options)
        {
            if (value is Events.MeetingEndActivity meetingEnd)
            {
                JsonSerializer.Serialize(writer, meetingEnd, options);
                return;
            }

            if (value is Events.MeetingStartActivity meetingStart)
            {
                JsonSerializer.Serialize(writer, meetingStart, options);
                return;
            }

            if (value is Events.MeetingParticipantJoinActivity meetingParticipantJoin)
            {
                JsonSerializer.Serialize(writer, meetingParticipantJoin, options);
                return;
            }

            if (value is Events.MeetingParticipantLeaveActivity meetingParticipantLeave)
            {
                JsonSerializer.Serialize(writer, meetingParticipantLeave, options);
                return;
            }

            if (value is Events.ReadReceiptActivity readReceipt)
            {
                JsonSerializer.Serialize(writer, readReceipt, options);
                return;
            }

            JsonSerializer.Serialize(writer, value, options);
        }
    }
}