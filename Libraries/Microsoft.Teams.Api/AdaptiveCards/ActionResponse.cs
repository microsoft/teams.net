using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.AdaptiveCards;

/// <summary>
/// Defines the structure that is returned as the result of an Invoke activity with
/// Name of 'adaptiveCard/action'.
/// </summary>
public class ActionResponse(ContentType contentType)
{
    /// <summary>
    /// The Card Action response status code.
    /// </summary>
    [JsonPropertyName("statusCode")]
    [JsonPropertyOrder(0)]
    public required int StatusCode { get; set; }

    /// <summary>
    /// The type of this response.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonPropertyOrder(1)]
    public ContentType Type { get; set; } = contentType;

    /// <summary>
    /// the response value
    /// </summary>
    [JsonPropertyName("value")]
    [JsonPropertyOrder(2)]
    public object? Value { get; set; }

    /// <summary>
    /// The request was successfully processed, and the response includes
    /// an Adaptive Card that the client should display in place of the current one
    /// </summary>
    public class Card : ActionResponse
    {
        /// <summary>
        /// The card response object.
        /// </summary>
        [JsonPropertyName("value")]
        [JsonPropertyOrder(2)]
        public new Cards.Card Value { get; set; }

        public Card(Cards.Card value) : base(ContentType.AdaptiveCard)
        {
            Value = value;
            StatusCode = 200;
        }
    }

    /// <summary>
    /// The request was successfully processed, and the response includes a message that the client should display
    /// </summary>
    public class Message : ActionResponse
    {
        /// <summary>
        /// the response message.
        /// </summary>
        [JsonPropertyName("value")]
        [JsonPropertyOrder(2)]
        public new string Value { get; set; }

        public Message(string value) : base(ContentType.Message)
        {
            Value = value;
            StatusCode = 200;
        }
    }

    /// <summary>
    /// `400`: The incoming request was invalid
    /// `500`: An unexpected error occurred
    /// </summary>
    public class Error : ActionResponse
    {
        /// <summary>
        /// The error response object.
        /// </summary>
        [JsonPropertyName("value")]
        [JsonPropertyOrder(2)]
        public new Error Value { get; set; }

        public Error(Error value, int statusCode = 400) : base(ContentType.Message)
        {
            Value = value;
            StatusCode = statusCode;
        }
    }

    /// <summary>
    /// The client needs to prompt the user to authenticate
    /// </summary>
    public class Login : ActionResponse
    {
        /// <summary>
        /// The auth response object.
        /// </summary>
        [JsonPropertyName("value")]
        [JsonPropertyOrder(2)]
        public new Cards.OAuthCard Value { get; set; }

        public Login(Cards.OAuthCard value) : base(ContentType.LoginRequest)
        {
            Value = value;
            StatusCode = 401;
        }
    }

    /// <summary>
    /// The authentication state passed by the client was incorrect and authentication failed
    /// </summary>
    public class IncorrectAuthCode : ActionResponse
    {
        public IncorrectAuthCode() : base(ContentType.IncorrectAuthCode)
        {
            StatusCode = 401;
        }
    }

    /// <summary>
    /// The SSO authentication flow failed
    /// </summary>
    public class PreConditionFailed : ActionResponse
    {
        /// <summary>
        /// The auth response object.
        /// </summary>
        [JsonPropertyName("value")]
        [JsonPropertyOrder(2)]
        public new Error Value { get; set; }

        public PreConditionFailed(Error value) : base(ContentType.PreConditionFailed)
        {
            Value = value;
            StatusCode = 412;
        }
    }
}