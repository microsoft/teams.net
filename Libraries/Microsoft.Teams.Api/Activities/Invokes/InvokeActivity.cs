// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities.Invokes;

[JsonConverter(typeof(JsonConverter<Name>))]
public partial class Name(string value) : StringEnum(value)
{
    public Type ToType()
    {
        if (IsExecuteAction) return typeof(ExecuteActionActivity);
        if (IsFileConsent) return typeof(FileConsentActivity);
        if (IsHandoff) return typeof(HandoffActivity);
        if (IsAdaptiveCard) return typeof(AdaptiveCardActivity);
        if (IsConfig) return typeof(ConfigActivity);
        if (IsMessage) return typeof(MessageActivity);
        if (IsMessageExtension) return typeof(MessageExtensionActivity);
        if (IsSignIn) return typeof(SignInActivity);
        if (IsTab) return typeof(TabActivity);
        if (IsTask) return typeof(TaskActivity);
        if (IsSearch) return typeof(SearchActivity);
        return typeof(InvokeActivity);
    }

    public string ToPrettyString()
    {
        var value = ToString();
        return $"{value.First().ToString().ToUpper()}{value.AsSpan(1).ToString()}";
    }

    [JsonConverter(typeof(JsonConverter<AdaptiveCards>))]
    public partial class AdaptiveCards(string value) : StringEnum(value)
    {

    }

    [JsonConverter(typeof(JsonConverter<Configs>))]
    public partial class Configs(string value) : StringEnum(value)
    {

    }

    [JsonConverter(typeof(JsonConverter<MessageExtensions>))]
    public partial class MessageExtensions(string value) : StringEnum(value)
    {

    }

    [JsonConverter(typeof(JsonConverter<Messages>))]
    public partial class Messages(string value) : StringEnum(value)
    {

    }

    [JsonConverter(typeof(JsonConverter<SignIn>))]
    public partial class SignIn(string value) : StringEnum(value)
    {

    }

    [JsonConverter(typeof(JsonConverter<Tabs>))]
    public partial class Tabs(string value) : StringEnum(value)
    {

    }

    [JsonConverter(typeof(JsonConverter<Tasks>))]
    public partial class Tasks(string value) : StringEnum(value)
    {

    }
}