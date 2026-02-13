// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Bot.Apps.Schema.Invokes;

/// <summary>
/// Adaptive card response types.
/// </summary>
public static class AdaptiveCardResponseType
{
    /// <summary>
    /// Message type - displays a message to the user.
    /// </summary>
    public const string Message = "application/vnd.microsoft.activity.message";

    /// <summary>
    /// Card type - updates the card with new content.
    /// </summary>
    public const string Card = "application/vnd.microsoft.card.adaptive";
}

/// <summary>
/// 
/// Response for adaptive card action activities.
/// </summary>
public class AdaptiveCardResponse
{
    /// <summary>
    /// HTTP status code for the response.
    /// </summary>
    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; } = 200;

    /// <summary>
    /// Type of response. See <see cref="AdaptiveCardResponseType"/> for common values.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Type { get; set; }

    /// <summary>
    /// Value for the response. Can be a string message or card content.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Value { get; set; }

    /// <summary>
    /// Creates a new builder for AdaptiveCardResponse.
    /// </summary>
    public static AdaptiveCardResponseBuilder CreateBuilder()
    {
        return new AdaptiveCardResponseBuilder();
    }

    /// <summary>
    /// Creates a InvokeResponse with a message response.
    /// </summary>
    /// <param name="message">The message to display to the user.</param>
    /// <param name="statusCode">The HTTP status code (default: 200).</param>
    public static InvokeResponse<AdaptiveCardResponse> CreateMessageResponse(string message, int statusCode = 200)
    {
        return new InvokeResponse<AdaptiveCardResponse>(statusCode, new AdaptiveCardResponse
        {
            StatusCode = statusCode,
            Type = AdaptiveCardResponseType.Message,
            Value = message
        });
    }

    /// <summary>
    /// Creates a InvokeResponse with a card response.
    /// </summary>
    /// <param name="card">The card content to display.</param>
    /// <param name="statusCode">The HTTP status code (default: 200).</param>
    public static InvokeResponse<AdaptiveCardResponse> CreateCardResponse(object card, int statusCode = 200)
    {
        return new InvokeResponse<AdaptiveCardResponse>(statusCode, new AdaptiveCardResponse
        {
            StatusCode = statusCode,
            Type = AdaptiveCardResponseType.Card,
            Value = card
        });
    }
}

/// <summary>
/// Builder for AdaptiveCardResponse.
/// </summary>
public class AdaptiveCardResponseBuilder
{
    private int _statusCode = 200;
    private string? _type;
    private object? _value;

    ///<summary>
    ///</summary>
    public AdaptiveCardResponseBuilder WithStatusCode(int statusCode)
    {
        _statusCode = statusCode;
        return this;
    }

    /// <summary>
    /// Sets the type of the response. See <see cref="AdaptiveCardResponseType"/> for common values.
    /// </summary>
    public AdaptiveCardResponseBuilder WithType(string type)
    {
        _type = type;
        return this;
    }

    /// <summary>
    /// Sets the value for the response.
    /// </summary>
    public AdaptiveCardResponseBuilder WithValue(object value)
    {
        _value = value;
        return this;
    }

    /// <summary>
    /// Builds the AdaptiveCardResponse and wraps it in a InvokeResponse.
    /// </summary>
    public InvokeResponse<AdaptiveCardResponse> Build()
    {
        return new InvokeResponse<AdaptiveCardResponse>(_statusCode, new AdaptiveCardResponse
        {
            StatusCode = _statusCode,
            Type = _type,
            Value = _value
        });
    }
}
