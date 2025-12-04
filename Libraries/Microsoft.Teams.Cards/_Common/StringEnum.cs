// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;

namespace Microsoft.Teams.Common;

[System.Text.Json.Serialization.JsonConverter(typeof(JsonConverter<StringEnum>))]
public class StringEnum(string value, bool caseSensitive = true) : ICloneable, IComparable, IComparable<string>, IEquatable<string>
{
    public string Value { get; set; } = value;

    private readonly bool _caseSensitive = caseSensitive;

    public object Clone() => new StringEnum(Value);
    public int CompareTo(object? value) => Value.CompareTo(value);
    public int CompareTo(string? value) => Value.CompareTo(value);
    public override string ToString() => Value;
    public override int GetHashCode() => Value.GetHashCode();
    public override bool Equals(object? value)
    {
        if (value is StringEnum stringEnum)
        {
            return Equals(stringEnum);
        }
        if (value is string str)
        {
            return Equals(str);
        }
        return false;
    }
    public bool Equals(StringEnum? value) => Value.Equals(value?.Value);
    public bool Equals(string? value)
    {
        if (!_caseSensitive)
        {
            return Value.ToLower().Equals(value?.ToLower());
        }

        return Value.Equals(value);
    }

    public static bool operator ==(StringEnum? a, StringEnum? b) => a?.Value == b?.Value;
    public static bool operator !=(StringEnum? a, StringEnum? b) => a?.Value != b?.Value;
    public static implicit operator string(StringEnum value) => value.Value;

    public class JsonConverter<TStringEnum> : System.Text.Json.Serialization.JsonConverter<TStringEnum>
        where TStringEnum : StringEnum
    {
        public override TStringEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();

            if (value is null)
            {
                throw new JsonException("value must not be null");
            }

            var res = Activator.CreateInstance(
                typeof(TStringEnum),
                [value]
            );

            if (res is null)
            {
                throw new JsonException($"could not create instance of '{typeof(TStringEnum)}'");
            }

            return (TStringEnum)res;
        }

        public override void Write(Utf8JsonWriter writer, TStringEnum value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Value);
        }
    }
}