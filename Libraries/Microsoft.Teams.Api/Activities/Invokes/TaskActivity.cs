using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities.Invokes;

public partial class Name : StringEnum
{
    public bool IsTask => Value.StartsWith("task/");
}

/// <summary>
/// Any Task Activity
/// </summary>
[JsonConverter(typeof(JsonConverter))]
public abstract class TaskActivity(Name.Tasks name) : InvokeActivity(new(name.Value))
{
    public Tasks.FetchActivity ToFetch() => (Tasks.FetchActivity)this;
    public Tasks.SubmitActivity ToSubmit() => (Tasks.SubmitActivity)this;

    public override object ToType(Type type, IFormatProvider? provider)
    {
        if (type == typeof(Tasks.FetchActivity)) return ToFetch();
        if (type == typeof(Tasks.SubmitActivity)) return ToSubmit();
        return this;
    }

    public new class JsonConverter : JsonConverter<TaskActivity>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return base.CanConvert(typeToConvert);
        }

        public override TaskActivity? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
                "task/fetch" => JsonSerializer.Deserialize<Tasks.FetchActivity>(element.ToString(), options),
                "task/submit" => JsonSerializer.Deserialize<Tasks.SubmitActivity>(element.ToString(), options),
                _ => JsonSerializer.Deserialize<TaskActivity>(element.ToString(), options)
            };
        }

        public override void Write(Utf8JsonWriter writer, TaskActivity value, JsonSerializerOptions options)
        {
            if (value is Tasks.FetchActivity fetch)
            {
                JsonSerializer.Serialize(writer, fetch, options);
                return;
            }

            if (value is Tasks.SubmitActivity submit)
            {
                JsonSerializer.Serialize(writer, submit, options);
                return;
            }

            JsonSerializer.Serialize(writer, value, options);
        }
    }
}