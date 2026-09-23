// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Apps.SocketMode;

/// <summary>
/// Provides the shared JSON configuration for Socket Mode frames.
/// </summary>
internal static class SocketModeJson
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Deserializes a Socket Mode JSON payload.
    /// </summary>
    /// <typeparam name="T">The payload type.</typeparam>
    /// <param name="json">The JSON payload.</param>
    /// <returns>The deserialized value, or <c>null</c> for a JSON null value.</returns>
    internal static T? Deserialize<T>(string json)
        => JsonSerializer.Deserialize<T>(json, SerializerOptions);

    /// <summary>
    /// Serializes a Socket Mode payload.
    /// </summary>
    /// <typeparam name="T">The payload type.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <returns>The serialized JSON payload.</returns>
    internal static string Serialize<T>(T value)
        => JsonSerializer.Serialize(value, SerializerOptions);
}
