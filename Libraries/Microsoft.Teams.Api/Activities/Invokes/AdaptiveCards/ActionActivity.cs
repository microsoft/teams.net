// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities.Invokes;

public partial class Name : StringEnum
{
    public partial class AdaptiveCards
    {
        public static readonly AdaptiveCards Action = new("adaptiveCard/action");
        public bool IsAction => Action.Equals(Value);
    }
}

public static partial class AdaptiveCards
{
    public class ActionActivity() : AdaptiveCardActivity(Name.AdaptiveCards.Action)
    {
        /// <summary>
        /// A value that is associated with the activity.
        /// </summary>
        [JsonPropertyName("value")]
        [JsonPropertyOrder(32)]
        public new required Api.AdaptiveCards.InvokeValue Value { get; set; }
    }
}