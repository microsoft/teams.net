// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Apps.Utils;

/// <summary>
/// Base type for string-backed value objects used by Teams schemas.
/// </summary>
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Matches existing Teams API naming")]
[JsonConverter(typeof(StringEnumJsonConverter<StringEnum>))]
public class StringEnum(string value)
{
    /// <summary>
    /// Gets or sets the string value.
    /// </summary>
    public string Value { get; set; } = value;

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => obj is StringEnum other
            && GetType() == other.GetType()
            && string.Equals(Value, other.Value, StringComparison.Ordinal);

    /// <inheritdoc />
    public override int GetHashCode()
        => HashCode.Combine(GetType(), Value);
}

/// <summary>
/// JSON converter for string-enum types.
/// </summary>
public class StringEnumJsonConverter<TStringEnum> : JsonConverter<TStringEnum>
    where TStringEnum : StringEnum
{
    /// <summary>
    /// Reads the value from JSON.
    /// </summary>
    public override TStringEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? value = reader.GetString();
        if (value is null)
        {
            throw new JsonException("value must not be null");
        }

        object? res = Activator.CreateInstance(typeof(TStringEnum), [value]);
        if (res is null)
        {
            throw new JsonException($"could not create instance of '{typeof(TStringEnum)}'");
        }

        return (TStringEnum)res;
    }

    /// <summary>
    /// Writes the value to JSON.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, TStringEnum value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);
        writer.WriteStringValue(value.Value);
    }
}
