// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Common;

[JsonConverter(typeof(UnionJsonConverterFactory))]
public partial interface IUnion<A, B> : IEquatable<Union<A, B>>
    where A : notnull
    where B : notnull
{
    public object Value { get; }

    public void Switch(Action<A> a, Action<B> b);
    public TResult Match<TResult>(Func<A, TResult> a, Func<B, TResult> b);
}

[JsonConverter(typeof(UnionJsonConverterFactory))]
public readonly struct Union<A, B> : IUnion<A, B>
    where A : notnull
    where B : notnull
{
    public object Value { get; }

    public Union(object value)
    {
        if (value is A a)
        {
            Value = a;
            return;
        }
        else if (value is B b)
        {
            Value = b;
            return;
        }

        throw new ArgumentException($"Union value {value.GetType().Name} is invalid");
    }

    public Union(A a)
    {
        Value = a;
    }

    public Union(B b)
    {
        Value = b;
    }

    public void Switch(Action<A> a, Action<B> b)
    {
        if (Value is A av)
        {
            a(av);
            return;
        }

        if (Value is B bv)
        {
            b(bv);
            return;
        }

        throw new InvalidOperationException();
    }

    public TResult Match<TResult>(Func<A, TResult> a, Func<B, TResult> b)
    {
        if (Value is A av)
        {
            return a(av);
        }

        if (Value is B bv)
        {
            return b(bv);
        }

        throw new InvalidOperationException();
    }

    public bool Equals(Union<A, B> value)
    {
        return Value.Equals(value.Value);
    }

    public override bool Equals(object? value)
    {
        return value is not null && value is Union<A, B> o && Equals(o);
    }

    public static bool operator ==(Union<A, B> left, Union<A, B> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Union<A, B> left, Union<A, B> right)
    {
        return !left.Equals(right);
    }

    public static implicit operator Union<A, B>(A value)
    {
        return new Union<A, B>(value);
    }

    public static implicit operator Union<A, B>(B value)
    {
        return new Union<A, B>(value);
    }

    public override int GetHashCode()
    {
        return Match(
            a => a.GetHashCode(),
            b => b.GetHashCode()
        );
    }

    public override string? ToString()
    {
        return Match(
            a => a.ToString(),
            b => b.ToString()
        );
    }
}

public class UnionJsonConverter<A, B> : JsonConverter<Union<A, B>>
    where A : notnull
    where B : notnull
{
    public override Union<A, B> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var element = JsonSerializer.Deserialize<JsonElement>(ref reader, options);

        if (element.ValueKind == JsonValueKind.Null)
        {
            throw new JsonException("Union value must not be null");
        }

        // Try to convert to type A first
        if (TryConvertJsonElement<A>(element, out var valueA, options))
        {
            return new Union<A, B>(valueA);
        }

        // Try to convert to type B
        if (TryConvertJsonElement<B>(element, out var valueB, options))
        {
            return new Union<A, B>(valueB);
        }

        throw new JsonException($"Unable to convert JSON value to Union<{typeof(A).Name}, {typeof(B).Name}>");
    }

    internal static bool TryConvertJsonElement<T>(JsonElement element, out T value, JsonSerializerOptions? options = null) where T : notnull
    {
        value = default!;

        try
        {
            var targetType = typeof(T);

            // Handle common primitive types
            if (targetType == typeof(string))
            {
                if (element.ValueKind == JsonValueKind.String)
                {
                    value = (T)(object)element.GetString()!;
                    return true;
                }
                return false;
            }

            if (targetType == typeof(int))
            {
                if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var intVal))
                {
                    value = (T)(object)intVal;
                    return true;
                }
                return false;
            }

            if (targetType == typeof(float))
            {
                if (element.ValueKind == JsonValueKind.Number && element.TryGetSingle(out var floatVal))
                {
                    value = (T)(object)floatVal;
                    return true;
                }
                return false;
            }

            if (targetType == typeof(double))
            {
                if (element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out var doubleVal))
                {
                    value = (T)(object)doubleVal;
                    return true;
                }
                return false;
            }

            if (targetType == typeof(bool))
            {
                if (element.ValueKind == JsonValueKind.True)
                {
                    value = (T)(object)true;
                    return true;
                }
                if (element.ValueKind == JsonValueKind.False)
                {
                    value = (T)(object)false;
                    return true;
                }
                return false;
            }

            // For complex types, try to deserialize directly
            if (element.ValueKind == JsonValueKind.Object || element.ValueKind == JsonValueKind.Array)
            {
                var deserializedValue = JsonSerializer.Deserialize<T>(element.GetRawText(), options);
                if (deserializedValue != null)
                {
                    value = deserializedValue;
                    return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public override void Write(Utf8JsonWriter writer, Union<A, B> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.Value, options);
    }
}

public class IUnionJsonConverter<A, B> : JsonConverter<IUnion<A, B>>
    where A : notnull
    where B : notnull
{
    public override IUnion<A, B> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var element = JsonSerializer.Deserialize<JsonElement>(ref reader, options);

        if (element.ValueKind == JsonValueKind.Null)
        {
            throw new JsonException("Cannot deserialize null to Union");
        }

        if (TryConvertJsonElement<A>(element, out var valueA, options))
            return new Union<A, B>(valueA);
        if (TryConvertJsonElement<B>(element, out var valueB, options))
            return new Union<A, B>(valueB);

        throw new JsonException($"Cannot convert JSON to Union<{typeof(A).Name}, {typeof(B).Name}>");
    }

    public override void Write(Utf8JsonWriter writer, IUnion<A, B> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.Value, options);
    }

    private static bool TryConvertJsonElement<T>(JsonElement element, out T value, JsonSerializerOptions? options = null) where T : notnull
    {
        value = default!;
        try
        {
            if (typeof(T) == typeof(string) && element.ValueKind == JsonValueKind.String)
            {
                value = (T)(object)element.GetString()!;
                return true;
            }
            if (typeof(T) == typeof(int) && element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var intVal))
            {
                value = (T)(object)intVal;
                return true;
            }
            if (typeof(T) == typeof(float) && element.ValueKind == JsonValueKind.Number && element.TryGetSingle(out var floatVal))
            {
                value = (T)(object)floatVal;
                return true;
            }
            if (typeof(T) == typeof(double) && element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out var doubleVal))
            {
                value = (T)(object)doubleVal;
                return true;
            }
            if (typeof(T) == typeof(bool) && element.ValueKind == JsonValueKind.True)
            {
                value = (T)(object)true;
                return true;
            }
            if (typeof(T) == typeof(bool) && element.ValueKind == JsonValueKind.False)
            {
                value = (T)(object)false;
                return true;
            }

            // For complex objects, try to deserialize directly
            if (element.ValueKind == JsonValueKind.Object || element.ValueKind == JsonValueKind.Array)
            {
                var jsonString = element.GetRawText();
                var result = JsonSerializer.Deserialize<T>(jsonString, options);
                if (result != null)
                {
                    value = result;
                    return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}

[JsonConverter(typeof(UnionJsonConverterFactory))]
public partial interface IUnion<A, B, C> : IEquatable<Union<A, B, C>>
    where A : notnull
    where B : notnull
    where C : notnull
{
    object Value { get; }

    public void Switch(Action<A> a, Action<B> b, Action<C> c);
    public TResult Match<TResult>(Func<A, TResult> a, Func<B, TResult> b, Func<C, TResult> c);
}

[JsonConverter(typeof(UnionJsonConverterFactory))]
public readonly struct Union<A, B, C> : IUnion<A, B, C>
    where A : notnull
    where B : notnull
    where C : notnull
{
    public object Value { get; }

    public Union(object value)
    {
        if (value is A a)
        {
            Value = a;
            return;
        }
        else if (value is B b)
        {
            Value = b;
            return;
        }
        else if (value is C c)
        {
            Value = c;
            return;
        }

        throw new ArgumentException($"Union value {value.GetType().Name} is invalid");
    }

    public Union(A a)
    {
        Value = a;
    }

    public Union(B b)
    {
        Value = b;
    }

    public Union(C c)
    {
        Value = c;
    }

    public void Switch(Action<A> a, Action<B> b, Action<C> c)
    {
        if (Value is A av)
        {
            a(av);
            return;
        }

        if (Value is B bv)
        {
            b(bv);
            return;
        }

        if (Value is C cv)
        {
            c(cv);
            return;
        }

        throw new InvalidOperationException();
    }

    public TResult Match<TResult>(Func<A, TResult> a, Func<B, TResult> b, Func<C, TResult> c)
    {
        if (Value is A av)
        {
            return a(av);
        }

        if (Value is B bv)
        {
            return b(bv);
        }

        if (Value is C cv)
        {
            return c(cv);
        }

        throw new InvalidOperationException();
    }

    public bool Equals(Union<A, B, C> value)
    {
        return Value.Equals(value.Value);
    }

    public override bool Equals(object? value)
    {
        return value is not null && value is Union<A, B, C> o && Equals(o);
    }

    public static bool operator ==(Union<A, B, C> left, Union<A, B, C> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Union<A, B, C> left, Union<A, B, C> right)
    {
        return !left.Equals(right);
    }

    public static implicit operator Union<A, B, C>(A value)
    {
        return new Union<A, B, C>(value);
    }

    public static implicit operator Union<A, B, C>(B value)
    {
        return new Union<A, B, C>(value);
    }

    public static implicit operator Union<A, B, C>(C value)
    {
        return new Union<A, B, C>(value);
    }

    public override int GetHashCode()
    {
        return Match(
            a => a.GetHashCode(),
            b => b.GetHashCode(),
            c => c.GetHashCode()
        );
    }

    public override string? ToString()
    {
        return Match(
            a => a.ToString(),
            b => b.ToString(),
            c => c.ToString()
        );
    }
}

public class UnionJsonConverter<A, B, C> : JsonConverter<Union<A, B, C>>
    where A : notnull
    where B : notnull
    where C : notnull
{
    public override Union<A, B, C> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var element = JsonSerializer.Deserialize<JsonElement>(ref reader, options);

        if (element.ValueKind == JsonValueKind.Null)
        {
            throw new JsonException("Union value must not be null");
        }

        // Try to convert to type A first
        if (UnionJsonConverter<A, B>.TryConvertJsonElement<A>(element, out var valueA, options))
        {
            return new Union<A, B, C>(valueA);
        }

        // Try to convert to type B
        if (UnionJsonConverter<A, B>.TryConvertJsonElement<B>(element, out var valueB, options))
        {
            return new Union<A, B, C>(valueB);
        }

        // Try to convert to type C
        if (UnionJsonConverter<A, B>.TryConvertJsonElement<C>(element, out var valueC, options))
        {
            return new Union<A, B, C>(valueC);
        }

        throw new JsonException($"Unable to convert JSON value to Union<{typeof(A).Name}, {typeof(B).Name}, {typeof(C).Name}>");
    }

    public override void Write(Utf8JsonWriter writer, Union<A, B, C> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.Value, options);
    }
}

public class IUnionJsonConverter<A, B, C> : JsonConverter<IUnion<A, B, C>>
    where A : notnull
    where B : notnull
    where C : notnull
{
    public override IUnion<A, B, C> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var element = JsonSerializer.Deserialize<JsonElement>(ref reader, options);

        if (element.ValueKind == JsonValueKind.Null)
        {
            throw new JsonException("Cannot deserialize null to Union");
        }

        if (TryConvertJsonElement<A>(element, out var valueA, options))
            return new Union<A, B, C>(valueA);
        if (TryConvertJsonElement<B>(element, out var valueB, options))
            return new Union<A, B, C>(valueB);
        if (TryConvertJsonElement<C>(element, out var valueC, options))
            return new Union<A, B, C>(valueC);

        throw new JsonException($"Cannot convert JSON to Union<{typeof(A).Name}, {typeof(B).Name}, {typeof(C).Name}>");
    }

    public override void Write(Utf8JsonWriter writer, IUnion<A, B, C> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.Value, options);
    }

    private static bool TryConvertJsonElement<T>(JsonElement element, out T value, JsonSerializerOptions? options = null) where T : notnull
    {
        value = default!;
        try
        {
            if (typeof(T) == typeof(string) && element.ValueKind == JsonValueKind.String)
            {
                value = (T)(object)element.GetString()!;
                return true;
            }
            if (typeof(T) == typeof(int) && element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var intVal))
            {
                value = (T)(object)intVal;
                return true;
            }
            if (typeof(T) == typeof(float) && element.ValueKind == JsonValueKind.Number && element.TryGetSingle(out var floatVal))
            {
                value = (T)(object)floatVal;
                return true;
            }
            if (typeof(T) == typeof(double) && element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out var doubleVal))
            {
                value = (T)(object)doubleVal;
                return true;
            }
            if (typeof(T) == typeof(bool) && element.ValueKind == JsonValueKind.True)
            {
                value = (T)(object)true;
                return true;
            }
            if (typeof(T) == typeof(bool) && element.ValueKind == JsonValueKind.False)
            {
                value = (T)(object)false;
                return true;
            }

            // For complex objects, try to deserialize directly
            if (element.ValueKind == JsonValueKind.Object || element.ValueKind == JsonValueKind.Array)
            {
                var jsonString = element.GetRawText();
                var result = JsonSerializer.Deserialize<T>(jsonString, options);
                if (result != null)
                {
                    value = result;
                    return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}

[JsonConverter(typeof(UnionJsonConverterFactory))]
public partial interface IUnion<A, B, C, D> : IEquatable<Union<A, B, C, D>>
    where A : notnull
    where B : notnull
    where C : notnull
    where D : notnull
{
    object Value { get; }

    public void Switch(Action<A> a, Action<B> b, Action<C> c, Action<D> d);
    public TResult Match<TResult>(Func<A, TResult> a, Func<B, TResult> b, Func<C, TResult> c, Func<D, TResult> d);
}

[JsonConverter(typeof(UnionJsonConverterFactory))]
public readonly struct Union<A, B, C, D> : IUnion<A, B, C, D>
    where A : notnull
    where B : notnull
    where C : notnull
    where D : notnull
{
    public object Value { get; }

    public Union(object value)
    {
        if (value is A a)
        {
            Value = a;
            return;
        }
        else if (value is B b)
        {
            Value = b;
            return;
        }
        else if (value is C c)
        {
            Value = c;
            return;
        }
        else if (value is D d)
        {
            Value = d;
            return;
        }

        throw new ArgumentException($"Union value {value.GetType().Name} is invalid");
    }

    public Union(A a)
    {
        Value = a;
    }

    public Union(B b)
    {
        Value = b;
    }

    public Union(C c)
    {
        Value = c;
    }

    public Union(D d)
    {
        Value = d;
    }

    public void Switch(Action<A> a, Action<B> b, Action<C> c, Action<D> d)
    {
        if (Value is A av)
        {
            a(av);
            return;
        }

        if (Value is B bv)
        {
            b(bv);
            return;
        }

        if (Value is C cv)
        {
            c(cv);
            return;
        }

        if (Value is D dv)
        {
            d(dv);
            return;
        }

        throw new InvalidOperationException();
    }

    public TResult Match<TResult>(Func<A, TResult> a, Func<B, TResult> b, Func<C, TResult> c, Func<D, TResult> d)
    {
        if (Value is A av)
        {
            return a(av);
        }

        if (Value is B bv)
        {
            return b(bv);
        }

        if (Value is C cv)
        {
            return c(cv);
        }

        if (Value is D dv)
        {
            return d(dv);
        }

        throw new InvalidOperationException();
    }

    public bool Equals(Union<A, B, C, D> value)
    {
        return Value.Equals(value.Value);
    }

    public override bool Equals(object? value)
    {
        return value is not null && value is Union<A, B, C> o && Equals(o);
    }

    public static bool operator ==(Union<A, B, C, D> left, Union<A, B, C, D> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Union<A, B, C, D> left, Union<A, B, C, D> right)
    {
        return !left.Equals(right);
    }

    public static implicit operator Union<A, B, C, D>(A value)
    {
        return new Union<A, B, C, D>(value);
    }

    public static implicit operator Union<A, B, C, D>(B value)
    {
        return new Union<A, B, C, D>(value);
    }

    public static implicit operator Union<A, B, C, D>(C value)
    {
        return new Union<A, B, C, D>(value);
    }

    public static implicit operator Union<A, B, C, D>(D value)
    {
        return new Union<A, B, C, D>(value);
    }

    public override int GetHashCode()
    {
        return Match(
            a => a.GetHashCode(),
            b => b.GetHashCode(),
            c => c.GetHashCode(),
            d => d.GetHashCode()
        );
    }

    public override string? ToString()
    {
        return Match(
            a => a.ToString(),
            b => b.ToString(),
            c => c.ToString(),
            d => d.ToString()
        );
    }
}

public class UnionJsonConverter<A, B, C, D> : JsonConverter<Union<A, B, C, D>>
    where A : notnull
    where B : notnull
    where C : notnull
    where D : notnull
{
    public override Union<A, B, C, D> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var element = JsonSerializer.Deserialize<JsonElement>(ref reader, options);

        if (element.ValueKind == JsonValueKind.Null)
        {
            throw new JsonException("Union value must not be null");
        }

        // Try to convert to type A first
        if (UnionJsonConverter<A, B>.TryConvertJsonElement<A>(element, out var valueA, options))
        {
            return new Union<A, B, C, D>(valueA);
        }

        // Try to convert to type B
        if (UnionJsonConverter<A, B>.TryConvertJsonElement<B>(element, out var valueB, options))
        {
            return new Union<A, B, C, D>(valueB);
        }

        // Try to convert to type C
        if (UnionJsonConverter<A, B>.TryConvertJsonElement<C>(element, out var valueC, options))
        {
            return new Union<A, B, C, D>(valueC);
        }

        // Try to convert to type D
        if (UnionJsonConverter<A, B>.TryConvertJsonElement<D>(element, out var valueD, options))
        {
            return new Union<A, B, C, D>(valueD);
        }

        throw new JsonException($"Unable to convert JSON value to Union<{typeof(A).Name}, {typeof(B).Name}, {typeof(C).Name}, {typeof(D).Name}>");
    }

    public override void Write(Utf8JsonWriter writer, Union<A, B, C, D> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.Value, options);
    }
}

public class IUnionJsonConverter<A, B, C, D> : JsonConverter<IUnion<A, B, C, D>>
    where A : notnull
    where B : notnull
    where C : notnull
    where D : notnull
{
    public override IUnion<A, B, C, D> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var element = JsonSerializer.Deserialize<JsonElement>(ref reader, options);

        if (element.ValueKind == JsonValueKind.Null)
        {
            throw new JsonException("Cannot deserialize null to Union");
        }

        if (TryConvertJsonElement<A>(element, out var valueA, options))
            return new Union<A, B, C, D>(valueA);
        if (TryConvertJsonElement<B>(element, out var valueB, options))
            return new Union<A, B, C, D>(valueB);
        if (TryConvertJsonElement<C>(element, out var valueC, options))
            return new Union<A, B, C, D>(valueC);
        if (TryConvertJsonElement<D>(element, out var valueD, options))
            return new Union<A, B, C, D>(valueD);

        throw new JsonException($"Cannot convert JSON to Union<{typeof(A).Name}, {typeof(B).Name}, {typeof(C).Name}, {typeof(D).Name}>");
    }

    public override void Write(Utf8JsonWriter writer, IUnion<A, B, C, D> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.Value, options);
    }

    private static bool TryConvertJsonElement<T>(JsonElement element, out T value, JsonSerializerOptions? options = null) where T : notnull
    {
        value = default!;
        try
        {
            if (typeof(T) == typeof(string) && element.ValueKind == JsonValueKind.String)
            {
                value = (T)(object)element.GetString()!;
                return true;
            }
            if (typeof(T) == typeof(int) && element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var intVal))
            {
                value = (T)(object)intVal;
                return true;
            }
            if (typeof(T) == typeof(float) && element.ValueKind == JsonValueKind.Number && element.TryGetSingle(out var floatVal))
            {
                value = (T)(object)floatVal;
                return true;
            }
            if (typeof(T) == typeof(double) && element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out var doubleVal))
            {
                value = (T)(object)doubleVal;
                return true;
            }
            if (typeof(T) == typeof(bool) && element.ValueKind == JsonValueKind.True)
            {
                value = (T)(object)true;
                return true;
            }
            if (typeof(T) == typeof(bool) && element.ValueKind == JsonValueKind.False)
            {
                value = (T)(object)false;
                return true;
            }

            // For complex objects, try to deserialize directly
            if (element.ValueKind == JsonValueKind.Object || element.ValueKind == JsonValueKind.Array)
            {
                var jsonString = element.GetRawText();
                var result = JsonSerializer.Deserialize<T>(jsonString, options);
                if (result != null)
                {
                    value = result;
                    return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}

public class UnionJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type type)
    {
        return (
            type.Name == typeof(Union<,>).Name ||
            type.Name == typeof(Union<,,>).Name ||
            type.Name == typeof(Union<,,,>).Name ||
            type.Name == typeof(IUnion<,>).Name ||
            type.Name == typeof(IUnion<,,>).Name ||
            type.Name == typeof(IUnion<,,,>).Name
        );
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var args = typeToConvert.GetGenericArguments();
        
        // Check if we're dealing with an interface type
        var isInterface = typeToConvert.IsInterface && typeToConvert.Name.StartsWith("IUnion");
        var converterPrefix = isInterface ? "IUnionJsonConverter" : "UnionJsonConverter";
        var name = $"{converterPrefix}`{args.Length}";
        
        var type = GetType().Assembly.GetTypes().Where(t => t.Name == name).FirstOrDefault();

        if (type is null)
        {
            throw new JsonException($"type '{name}' not found");
        }

        var converterType = type.MakeGenericType(args);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}