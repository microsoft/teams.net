// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Teams.Apps.Schema;

namespace Microsoft.Teams.Apps.State;

/// <summary>
/// Serialization helpers for state scope documents. A state scope is an open-typed bag
/// (<c>Dictionary&lt;string, object?&gt;</c>), so serialization is fundamentally reflection-based.
/// The canonical <see cref="TeamsActivityJsonContext"/> supplies fast, source-generated metadata for
/// the primitives and <see cref="JsonElement"/> values that commonly appear; the combined reflection
/// resolver handles arbitrary user POCO values.
/// </summary>
internal static class StateSerializer
{
    /// <summary>
    /// Serializer options reusing the canonical Teams source-generated context for known primitive and
    /// framework types, combined with a reflection resolver so arbitrary user POCO values still
    /// serialize. State stores user-defined types, so it cannot be a closed-world, fully source-generated
    /// serializer like the activity pipeline — hence the reflection fallback.
    /// </summary>
    internal static readonly JsonSerializerOptions Options = new()
    {
        TypeInfoResolver = JsonTypeInfoResolver.Combine(TeamsActivityJsonContext.Default, new DefaultJsonTypeInfoResolver()),
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Serializes a scope's values to canonical UTF-8 JSON (used for change detection and storage).</summary>
    internal static byte[] Serialize(IDictionary<string, object?> values)
        => JsonSerializer.SerializeToUtf8Bytes(values, Options);

    /// <summary>Deserializes a scope's values from UTF-8 JSON. Values are returned as <see cref="JsonElement"/>.</summary>
    internal static Dictionary<string, object?> Deserialize(ReadOnlySpan<byte> utf8Json)
        => JsonSerializer.Deserialize<Dictionary<string, object?>>(utf8Json, Options) ?? [];

    /// <summary>Converts a stored <see cref="JsonElement"/> to the requested type.</summary>
    internal static T? Convert<T>(JsonElement element)
        => element.Deserialize<T>(Options);
}
