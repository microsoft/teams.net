// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities.Invokes;

public partial class Name : StringEnum
{
    public partial class MessageExtensions : StringEnum
    {
        public static readonly MessageExtensions SubmitAction = new("composeExtension/submitAction");
        public bool IsSubmitAction => SubmitAction.Equals(Value);
    }
}

public static partial class MessageExtensions
{
    public class SubmitActionActivity() : MessageExtensionActivity(Name.MessageExtensions.SubmitAction)
    {
        /// <summary>
        /// A value that is associated with the activity.
        /// </summary>
        [JsonPropertyName("value")]
        [JsonPropertyOrder(32)]
        public new required Api.MessageExtensions.Action Value
        {
            get => (Api.MessageExtensions.Action)base.Value!;
            set => base.Value = value;
        }
    }
}