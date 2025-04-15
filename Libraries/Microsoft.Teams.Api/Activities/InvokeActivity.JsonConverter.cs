using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Activities;

public partial class InvokeActivity
{
    public new class JsonConverter : JsonConverter<InvokeActivity>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return base.CanConvert(typeToConvert);
        }

        public override InvokeActivity? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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

            if (name.StartsWith("adaptiveCard/"))
            {
                return JsonSerializer.Deserialize<Invokes.AdaptiveCardActivity>(element.ToString(), options);
            }

            if (name.StartsWith("config/"))
            {
                return JsonSerializer.Deserialize<Invokes.ConfigActivity>(element.ToString(), options);
            }

            if (name.StartsWith("composeExtension/"))
            {
                return JsonSerializer.Deserialize<Invokes.MessageExtensionActivity>(element.ToString(), options);
            }

            if (name.StartsWith("message/"))
            {
                return JsonSerializer.Deserialize<Invokes.MessageActivity>(element.ToString(), options);
            }

            if (name.StartsWith("signin/"))
            {
                return JsonSerializer.Deserialize<Invokes.SignInActivity>(element.ToString(), options);
            }

            if (name.StartsWith("tab/"))
            {
                return JsonSerializer.Deserialize<Invokes.TabActivity>(element.ToString(), options);
            }

            if (name.StartsWith("task/"))
            {
                return JsonSerializer.Deserialize<Invokes.TaskActivity>(element.ToString(), options);
            }

            return name switch
            {
                "actionableMessage/executeAction" => JsonSerializer.Deserialize<Invokes.ExecuteActionActivity>(element.ToString(), options),
                "fileConsent/invoke" => JsonSerializer.Deserialize<Invokes.FileConsentActivity>(element.ToString(), options),
                "handoff/action" => JsonSerializer.Deserialize<Invokes.HandoffActivity>(element.ToString(), options),
                _ => JsonSerializer.Deserialize<InvokeActivity>(element.ToString(), options)
            };
        }

        public override void Write(Utf8JsonWriter writer, InvokeActivity value, JsonSerializerOptions options)
        {
            if (value is Invokes.AdaptiveCardActivity adaptiveCard)
            {
                JsonSerializer.Serialize(writer, adaptiveCard, options);
                return;
            }

            if (value is Invokes.ConfigActivity config)
            {
                JsonSerializer.Serialize(writer, config, options);
                return;
            }

            if (value is Invokes.MessageExtensionActivity messageExtension)
            {
                JsonSerializer.Serialize(writer, messageExtension, options);
                return;
            }

            if (value is Invokes.MessageActivity message)
            {
                JsonSerializer.Serialize(writer, message, options);
                return;
            }

            if (value is Invokes.SignInActivity signIn)
            {
                JsonSerializer.Serialize(writer, signIn, options);
                return;
            }

            if (value is Invokes.TabActivity tab)
            {
                JsonSerializer.Serialize(writer, tab, options);
                return;
            }

            if (value is Invokes.TaskActivity task)
            {
                JsonSerializer.Serialize(writer, task, options);
                return;
            }

            JsonSerializer.Serialize(writer, value, options);
        }
    }
}