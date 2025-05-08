using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Common;

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
        var value = JsonSerializer.Deserialize<object>(ref reader, options);

        if (value is null)
        {
            throw new JsonException("Union value must not be null");
        }

        return new Union<A, B>(value);
    }

    public override void Write(Utf8JsonWriter writer, Union<A, B> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.Value, options);
    }
}

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
        var value = JsonSerializer.Deserialize<object>(ref reader, options);

        if (value is null)
        {
            throw new JsonException("Union value must not be null");
        }

        return new Union<A, B, C>(value);
    }

    public override void Write(Utf8JsonWriter writer, Union<A, B, C> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.Value, options);
    }
}

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
        var value = JsonSerializer.Deserialize<object>(ref reader, options);

        if (value is null)
        {
            throw new JsonException("Union value must not be null");
        }

        return new Union<A, B, C, D>(value);
    }

    public override void Write(Utf8JsonWriter writer, Union<A, B, C, D> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.Value, options);
    }
}

public class UnionJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type type)
    {
        return (
            type.Name == typeof(Union<,>).Name ||
            type.Name == typeof(Union<,,>).Name ||
            type.Name == typeof(Union<,,,>).Name
        );
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var args = typeToConvert.GetGenericArguments();
        var name = $"UnionJsonConverter`{args.Length}";
        var type = GetType().Assembly.GetTypes().Where(t => t.Name == name).FirstOrDefault();

        if (type is null)
        {
            throw new JsonException($"type '{name}' not found");
        }

        var converterType = type.MakeGenericType(args);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}