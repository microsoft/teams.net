// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities.Invokes;

public partial class Name : StringEnum
{
    public partial class MessageExtensions : StringEnum
    {
        public static readonly MessageExtensions QuerySettingsUrl = new("composeExtension/querySettingsUrl");
        public bool IsQuerySettingsUrl => QuerySettingsUrl.Equals(Value);
    }
}

public static partial class MessageExtensions
{
    public class QuerySettingsUrlActivity() : MessageExtensionActivity(Name.MessageExtensions.QuerySettingsUrl)
    {
        /// <summary>
        /// A value that is associated with the activity.
        /// </summary>
        [JsonPropertyName("value")]
        [JsonPropertyOrder(32)]
        public new required Api.MessageExtensions.Query Value { get; set; }
    }
}