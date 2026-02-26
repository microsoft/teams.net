// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.SignIn;

/// <summary>
/// Signin state (part of signin action auth flow) verification invoke query
/// </summary>
public class StateVerifyQuery
{
    /// <summary>
    /// The state string originally received when the
    /// signin web flow is finished with a state posted back to client via tab SDK
    /// microsoftTeams.authentication.notifySuccess(state).
    /// Can be either a string or a JSON object depending on the platform (Android/iOS may send objects).
    /// When a JSON object is received, it is automatically serialized to a JSON string.
    /// </summary>
    [JsonPropertyName("state")]
    [JsonPropertyOrder(0)]
    [JsonConverter(typeof(StringOrObjectConverter))]
    public string? State { get; set; }

    /// <summary>
    /// Custom JSON converter that handles both string and object values for the State property.
    /// When deserializing, if the value is a string, it returns the string value.
    /// If the value is a JSON object (or any other type), it serializes it to a JSON string.
    /// </summary>
    private class StringOrObjectConverter : JsonConverter<string?>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                return reader.GetString();
            }

            // For any other token type (object, array, number, etc.), read as JsonElement and serialize
            using var doc = JsonDocument.ParseValue(ref reader);
            return JsonSerializer.Serialize(doc.RootElement);
        }

        public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStringValue(value);
            }
        }
    }
}