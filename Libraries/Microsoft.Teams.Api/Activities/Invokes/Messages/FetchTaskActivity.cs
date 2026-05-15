// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities.Invokes;

public partial class Name : StringEnum
{
    public partial class Messages : StringEnum
    {
        public static readonly Messages FetchTask = new("message/fetchTask");
        public bool IsFetchTask => FetchTask.Equals(Value);
    }
}

/// <summary>
/// The feedback button the user clicked.
/// </summary>
[JsonConverter(typeof(JsonConverter<Reaction>))]
public partial class Reaction(string value) : StringEnum(value)
{
    public static readonly Reaction Like = new("like");
    public bool IsLike => Like.Equals(Value);

    public static readonly Reaction Dislike = new("dislike");
    public bool IsDislike => Dislike.Equals(Value);
}

public static partial class Messages
{
    /// <summary>
    /// Sent when a message has a custom feedback loop and the user clicks a
    /// feedback button. The bot should respond with a task module (dialog) to
    /// collect feedback.
    /// </summary>
    public class FetchTaskActivity() : MessageActivity(Name.Messages.FetchTask)
    {
        /// <summary>
        /// A value that is associated with the activity.
        /// </summary>
        [JsonPropertyName("value")]
        [JsonPropertyOrder(32)]
        public new required FetchTaskValue Value
        {
            get => (FetchTaskValue)base.Value!;
            set => base.Value = value;
        }

        /// <summary>
        /// The value associated with a message fetch task.
        /// </summary>
        public class FetchTaskValue
        {
            /// <summary>
            /// The data payload containing action name and value.
            /// </summary>
            [JsonPropertyName("data")]
            [JsonPropertyOrder(0)]
            public required FetchTaskData Data { get; set; }
        }

        /// <summary>
        /// The data payload nested inside the fetch task value.
        /// </summary>
        public class FetchTaskData
        {
            /// <summary>
            /// The name of the action.
            /// </summary>
            [JsonPropertyName("actionName")]
            [JsonPropertyOrder(0)]
            public string ActionName { get; set; } = "feedback";

            /// <summary>
            /// Contains the user's reaction.
            /// </summary>
            [JsonPropertyName("actionValue")]
            [JsonPropertyOrder(1)]
            public required FetchTaskActionValue ActionValue { get; set; }
        }

        /// <summary>
        /// The nested action value containing the user's reaction.
        /// </summary>
        public class FetchTaskActionValue
        {
            /// <summary>
            /// The feedback button the user clicked.
            /// </summary>
            [JsonPropertyName("reaction")]
            [JsonPropertyOrder(0)]
            public required Reaction Reaction { get; set; }
        }
    }
}
