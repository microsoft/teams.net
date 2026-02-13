// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Bot.Apps.Schema;

/// <summary>
/// Adaptive card invoke response types.
/// </summary>
public static class AdaptiveCardInvokeResponseType
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
/// Response for adaptive card action invoke activities.
/// </summary>
public class AdaptiveCardInvokeResponse
{
    /// <summary>
    /// HTTP status code for the response.
    /// </summary>
    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; } = 200;

    /// <summary>
    /// Type of response. See <see cref="AdaptiveCardInvokeResponseType"/> for common values.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Value for the response. Can be a string message or card content.
    /// </summary>
    [JsonPropertyName("value")]
    public object? Value { get; set; }

    /// <summary>
    /// Creates a new builder for AdaptiveCardInvokeResponse.
    /// </summary>
    public static AdaptiveCardInvokeResponseBuilder CreateBuilder()
    {
        return new AdaptiveCardInvokeResponseBuilder();
    }

    /// <summary>
    /// Creates a message response with default status code 200.
    /// </summary>
    /// <param name="message">The message to display to the user.</param>
    public static AdaptiveCardInvokeResponse CreateMessageResponse(string message)
    {
        return new AdaptiveCardInvokeResponse
        {
            StatusCode = 200,
            Type = AdaptiveCardInvokeResponseType.Message,
            Value = message
        };
    }

    /// <summary>
    /// Creates a card response with default status code 200.
    /// </summary>
    /// <param name="card">The card content to display.</param>
    public static AdaptiveCardInvokeResponse CreateCardResponse(object card)
    {
        return new AdaptiveCardInvokeResponse
        {
            StatusCode = 200,
            Type = AdaptiveCardInvokeResponseType.Card,
            Value = card
        };
    }
}

/// <summary>
/// Builder for AdaptiveCardInvokeResponse.
/// </summary>
public class AdaptiveCardInvokeResponseBuilder
{
    private int _statusCode = 200;
    private string? _type;
    private object? _value;

    /// <summary>
    /// Sets the status code for the response.
    /// </summary>
    public AdaptiveCardInvokeResponseBuilder WithStatusCode(int statusCode)
    {
        _statusCode = statusCode;
        return this;
    }

    /// <summary>
    /// Sets the type of the response. See <see cref="AdaptiveCardInvokeResponseType"/> for common values.
    /// </summary>
    public AdaptiveCardInvokeResponseBuilder WithType(string type)
    {
        _type = type;
        return this;
    }

    /// <summary>
    /// Sets the value for the response.
    /// </summary>
    public AdaptiveCardInvokeResponseBuilder WithValue(object value)
    {
        _value = value;
        return this;
    }

    /// <summary>
    /// Builds the AdaptiveCardInvokeResponse.
    /// </summary>
    public AdaptiveCardInvokeResponse Build()
    {
        return new AdaptiveCardInvokeResponse
        {
            StatusCode = _statusCode,
            Type = _type,
            Value = _value
        };
    }
}
