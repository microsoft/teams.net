// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Tabs;

/// <summary>
/// Choice of action options when responding to the tab/fetch message.
/// </summary>
[JsonConverter(typeof(JsonConverter<ActionType>))]
public class ActionType(string value) : StringEnum(value)
{
    public static readonly ActionType Continue = new("continue");
    public bool IsContinue => Continue.Equals(Value);

    public static readonly ActionType Auth = new("auth");
    public bool IsAuth => Auth.Equals(Value);

    public static readonly ActionType SilentAuth = new("silentAuth");
    public bool IsSilentAuth => SilentAuth.Equals(Value);
}

/// <summary>
/// Envelope for Card Tab Response Payload.
/// </summary>
public class Response(Response.Payload tab)
{
    /// <summary>
    /// The response to the tab/fetch message.
    /// </summary>
    [JsonPropertyName("tab")]
    [JsonPropertyOrder(0)]
    public Payload Tab { get; set; } = tab;

    /// <summary>
    /// Payload for Tab Response.
    /// </summary>
    public class Payload
    {
        /// <summary>
        /// Choice of action options when responding to the tab/fetch message.
        /// </summary>
        [JsonPropertyName("type")]
        [JsonPropertyOrder(0)]
        public ActionType? Type { get; set; }

        /// <summary>
        /// The TabResponseCards to send when responding to
        /// tab/fetch activity with type of 'continue'.
        /// </summary>
        [JsonPropertyName("value")]
        [JsonPropertyOrder(1)]
        public CardsPayload? Value { get; set; }

        /// <summary>
        /// The Suggested Actions for this card tab.
        /// </summary>
        [JsonPropertyName("suggestedActions")]
        [JsonPropertyOrder(2)]
        public SuggestedActions? SuggestedActions { get; set; }

        /// <summary>
        /// Envelope for cards for a Tab request.
        /// </summary>
        public class CardPayload
        {
            /// <summary>
            /// The adaptive card for this card tab response.
            /// </summary>
            [JsonPropertyName("card")]
            [JsonPropertyOrder(0)]
            public IDictionary<string, object?> Card { get; set; } = new Dictionary<string, object?>();
        }

        /// <summary>
        /// Envelope for cards for a TabResponse.
        /// </summary>
        public class CardsPayload
        {
            /// <summary>
            /// Adaptive cards for this card tab response.
            /// </summary>
            [JsonPropertyName("cards")]
            [JsonPropertyOrder(0)]
            public IList<CardPayload> Cards { get; set; } = [];
        }
    }
}