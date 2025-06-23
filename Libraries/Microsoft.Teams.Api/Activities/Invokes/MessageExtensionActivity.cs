// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities.Invokes;

public partial class Name : StringEnum
{
    public bool IsMessageExtension => Value.StartsWith("composeExtension/");
}

/// <summary>
/// Any Message Extension Activity
/// </summary>
[JsonConverter(typeof(JsonConverter))]
public abstract class MessageExtensionActivity(Name.MessageExtensions name) : InvokeActivity(new(name.Value))
{
    public MessageExtensions.AnonQueryLinkActivity ToAnonQueryLink() => (MessageExtensions.AnonQueryLinkActivity)this;
    public MessageExtensions.CardButtonClickedActivity ToCardButtonClicked() => (MessageExtensions.CardButtonClickedActivity)this;
    public MessageExtensions.FetchTaskActivity ToFetchTask() => (MessageExtensions.FetchTaskActivity)this;
    public MessageExtensions.QueryActivity ToQuery() => (MessageExtensions.QueryActivity)this;
    public MessageExtensions.QueryLinkActivity ToQueryLink() => (MessageExtensions.QueryLinkActivity)this;
    public MessageExtensions.QuerySettingsUrlActivity ToQuerySettingsUrl() => (MessageExtensions.QuerySettingsUrlActivity)this;
    public MessageExtensions.SelectItemActivity ToSelectItem() => (MessageExtensions.SelectItemActivity)this;
    public MessageExtensions.SettingActivity ToSetting() => (MessageExtensions.SettingActivity)this;
    public MessageExtensions.SubmitActionActivity ToSubmitAction() => (MessageExtensions.SubmitActionActivity)this;

    public override object ToType(Type type, IFormatProvider? provider)
    {
        if (type == typeof(MessageExtensions.AnonQueryLinkActivity)) return ToAnonQueryLink();
        if (type == typeof(MessageExtensions.CardButtonClickedActivity)) return ToCardButtonClicked();
        if (type == typeof(MessageExtensions.FetchTaskActivity)) return ToFetchTask();
        if (type == typeof(MessageExtensions.QueryActivity)) return ToQuery();
        if (type == typeof(MessageExtensions.QueryLinkActivity)) return ToQueryLink();
        if (type == typeof(MessageExtensions.QuerySettingsUrlActivity)) return ToQuerySettingsUrl();
        if (type == typeof(MessageExtensions.SelectItemActivity)) return ToSelectItem();
        if (type == typeof(MessageExtensions.SettingActivity)) return ToSetting();
        if (type == typeof(MessageExtensions.SubmitActionActivity)) return ToSubmitAction();
        return this;
    }

    public new class JsonConverter : JsonConverter<MessageExtensionActivity>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return base.CanConvert(typeToConvert);
        }

        public override MessageExtensionActivity? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
                "composeExtension/anonymousQueryLink" => JsonSerializer.Deserialize<MessageExtensions.AnonQueryLinkActivity>(element.ToString(), options),
                "composeExtension/onCardButtonClicked" => JsonSerializer.Deserialize<MessageExtensions.CardButtonClickedActivity>(element.ToString(), options),
                "composeExtension/fetchTask" => JsonSerializer.Deserialize<MessageExtensions.FetchTaskActivity>(element.ToString(), options),
                "composeExtension/query" => JsonSerializer.Deserialize<MessageExtensions.QueryActivity>(element.ToString(), options),
                "composeExtension/queryLink" => JsonSerializer.Deserialize<MessageExtensions.QueryLinkActivity>(element.ToString(), options),
                "composeExtension/querySettingsUrl" => JsonSerializer.Deserialize<MessageExtensions.QuerySettingsUrlActivity>(element.ToString(), options),
                "composeExtension/selectItem" => JsonSerializer.Deserialize<MessageExtensions.SelectItemActivity>(element.ToString(), options),
                "composeExtension/setting" => JsonSerializer.Deserialize<MessageExtensions.SettingActivity>(element.ToString(), options),
                "composeExtension/submitAction" => JsonSerializer.Deserialize<MessageExtensions.SubmitActionActivity>(element.ToString(), options),
                _ => JsonSerializer.Deserialize<MessageExtensionActivity>(element.ToString(), options)
            };
        }

        public override void Write(Utf8JsonWriter writer, MessageExtensionActivity value, JsonSerializerOptions options)
        {
            if (value is MessageExtensions.AnonQueryLinkActivity anonQueryLink)
            {
                JsonSerializer.Serialize(writer, anonQueryLink, options);
                return;
            }

            if (value is MessageExtensions.CardButtonClickedActivity cardButtonClicked)
            {
                JsonSerializer.Serialize(writer, cardButtonClicked, options);
                return;
            }

            if (value is MessageExtensions.FetchTaskActivity fetchTask)
            {
                JsonSerializer.Serialize(writer, fetchTask, options);
                return;
            }

            if (value is MessageExtensions.QueryActivity query)
            {
                JsonSerializer.Serialize(writer, query, options);
                return;
            }

            if (value is MessageExtensions.QueryLinkActivity queryLink)
            {
                JsonSerializer.Serialize(writer, queryLink, options);
                return;
            }

            if (value is MessageExtensions.QuerySettingsUrlActivity querySettingsUrl)
            {
                JsonSerializer.Serialize(writer, querySettingsUrl, options);
                return;
            }

            if (value is MessageExtensions.SelectItemActivity selectItem)
            {
                JsonSerializer.Serialize(writer, selectItem, options);
                return;
            }

            if (value is MessageExtensions.SettingActivity setting)
            {
                JsonSerializer.Serialize(writer, setting, options);
                return;
            }

            if (value is MessageExtensions.SubmitActionActivity submitAction)
            {
                JsonSerializer.Serialize(writer, submitAction, options);
                return;
            }

            JsonSerializer.Serialize(writer, value, options);
        }
    }
}