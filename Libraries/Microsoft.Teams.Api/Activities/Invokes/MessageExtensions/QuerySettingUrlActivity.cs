// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities.Invokes;

public partial class Name : StringEnum
{
    public partial class MessageExtensions : StringEnum
    {
        public static readonly MessageExtensions QuerySettingUrl = new("composeExtension/querySettingUrl");
        public bool IsQuerySettingsUrl => QuerySettingUrl.Equals(Value);
    }
}

public static partial class MessageExtensions
{
    public class QuerySettingUrlActivity() : MessageExtensionActivity(Name.MessageExtensions.QuerySettingUrl)
    {
        /// <summary>
        /// A value that is associated with the activity.
        /// </summary>
        [JsonPropertyName("value")]
        [JsonPropertyOrder(32)]
        public new required Api.MessageExtensions.Query Value
        {
            get => (Api.MessageExtensions.Query)base.Value!;
            set => base.Value = value;
        }
    }
}