// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities.Invokes;

public partial class Name : StringEnum
{
    public partial class Messages : StringEnum
    {
        public static readonly Messages SubmitAction = new("message/submitAction");
        public bool IsSubmitAction => SubmitAction.Equals(Value);
    }
}

public static partial class Messages
{
    public class SubmitActionActivity() : MessageActivity(Name.Messages.SubmitAction)
    {
        /// <summary>
        /// A value that is associated with the activity.
        /// </summary>
        [JsonPropertyName("value")]
        [JsonPropertyOrder(32)]
        public new required SubmitActionValue Value { get; set; }

        /// <summary>
        /// The Submit Action
        /// </summary>
        public class SubmitActionValue
        {
            /// <summary>
            /// Action name.
            /// </summary>
            [JsonPropertyName("actionName")]
            [JsonPropertyOrder(0)]
            public required string ActionName { get; set; }

            /// <summary>
            /// Action value.
            /// </summary>
            [JsonPropertyName("actionValue")]
            [JsonPropertyOrder(1)]
            public object? ActionValue { get; set; }
        }
    }
}