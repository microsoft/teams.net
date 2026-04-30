// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Teams.Api.Entities;

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

            if (name is null)
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

            if (name.StartsWith("handOff/"))
            {
                return JsonSerializer.Deserialize<Invokes.HandoffActivity>(element.ToString(), options);
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
                "application/search" => JsonSerializer.Deserialize<Invokes.SearchActivity>(element.ToString(), options),
                _ => DeserializeBase(name, element, options)
            };
        }

        private static InvokeActivity DeserializeBase(string name, JsonElement element, JsonSerializerOptions options)
        {
            var activity = new InvokeActivity(new Invokes.Name(name));

            if (element.TryGetProperty("id", out var idEl) && idEl.ValueKind != JsonValueKind.Null)
            {
                activity.Id = idEl.Deserialize<string>(options)!;
            }

            if (element.TryGetProperty("replyToId", out var replyToIdEl) && replyToIdEl.ValueKind != JsonValueKind.Null)
            {
                activity.ReplyToId = replyToIdEl.Deserialize<string>(options);
            }

            if (element.TryGetProperty("channelId", out var channelIdEl) && channelIdEl.ValueKind != JsonValueKind.Null)
            {
                activity.ChannelId = channelIdEl.Deserialize<ChannelId>(options)!;
            }

            if (element.TryGetProperty("from", out var fromEl) && fromEl.ValueKind != JsonValueKind.Null)
            {
                activity.From = fromEl.Deserialize<Account>(options)!;
            }

            if (element.TryGetProperty("recipient", out var recipientEl) && recipientEl.ValueKind != JsonValueKind.Null)
            {
                activity.Recipient = recipientEl.Deserialize<Account>(options)!;
            }

            if (element.TryGetProperty("conversation", out var conversationEl) && conversationEl.ValueKind != JsonValueKind.Null)
            {
                activity.Conversation = conversationEl.Deserialize<Conversation>(options)!;
            }

            if (element.TryGetProperty("relatesTo", out var relatesToEl) && relatesToEl.ValueKind != JsonValueKind.Null)
            {
                activity.RelatesTo = relatesToEl.Deserialize<ConversationReference>(options);
            }

            if (element.TryGetProperty("serviceUrl", out var serviceUrlEl) && serviceUrlEl.ValueKind != JsonValueKind.Null)
            {
                activity.ServiceUrl = serviceUrlEl.Deserialize<string>(options);
            }

            if (element.TryGetProperty("locale", out var localeEl) && localeEl.ValueKind != JsonValueKind.Null)
            {
                activity.Locale = localeEl.Deserialize<string>(options);
            }

            if (element.TryGetProperty("timestamp", out var timestampEl) && timestampEl.ValueKind != JsonValueKind.Null)
            {
                activity.Timestamp = timestampEl.Deserialize<DateTime?>(options);
            }

            if (element.TryGetProperty("localTimestamp", out var localTimestampEl) && localTimestampEl.ValueKind != JsonValueKind.Null)
            {
                activity.LocalTimestamp = localTimestampEl.Deserialize<DateTime?>(options);
            }

            if (element.TryGetProperty("entities", out var entitiesEl) && entitiesEl.ValueKind != JsonValueKind.Null)
            {
                activity.Entities = entitiesEl.Deserialize<IList<IEntity>>(options);
            }

            if (element.TryGetProperty("channelData", out var channelDataEl) && channelDataEl.ValueKind != JsonValueKind.Null)
            {
                activity.ChannelData = channelDataEl.Deserialize<ChannelData>(options);
            }

            if (element.TryGetProperty("value", out var valueEl) && valueEl.ValueKind != JsonValueKind.Null)
            {
                activity.Value = valueEl.Deserialize<object>(options);
            }

            return activity;
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

            if (value is Invokes.HandoffActivity handoff)
            {
                JsonSerializer.Serialize(writer, handoff, options);
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