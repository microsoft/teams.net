using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Cards;

internal sealed class CardElementJsonConverter : JsonConverter<CardElement>
{
    // Map JSON "type" to concrete CardElement type.
    // Extend this as you add more CardElement types.
    private static readonly Dictionary<string, Type> _typeMap = new(StringComparer.Ordinal)
    {
        // Common AC elements (add more as needed)
        ["TextBlock"] = typeof(TextBlock),
        ["Image"] = typeof(Image),
        ["Container"] = typeof(Container),
        ["ActionSet"] = typeof(ActionSet),
        ["RichTextBlock"] = typeof(RichTextBlock),
        ["FactSet"] = typeof(FactSet),
        ["ImageSet"] = typeof(ImageSet),

        // Inputs (if present in your model)
        ["Input.Text"] = typeof(TextInput),
        ["Input.Number"] = typeof(NumberInput),
        ["Input.Date"] = typeof(DateInput),
        ["Input.Time"] = typeof(TimeInput),
        ["Input.Toggle"] = typeof(ToggleInput),
        ["Input.ChoiceSet"] = typeof(ChoiceSetInput)
    };

    public override CardElement? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("type", out var typeProp) || typeProp.ValueKind != JsonValueKind.String)
        {
            throw new JsonException("CardElement payload must contain a string 'type' property.");
        }

        var typeName = typeProp.GetString();
        if (string.IsNullOrWhiteSpace(typeName))
        {
            throw new JsonException("CardElement 'type' property cannot be null or empty.");
        }

        if (!_typeMap.TryGetValue(typeName, out var concreteType))
        {
            // Fallback: try to resolve by class name equality (e.g., "TextBlock" -> Microsoft.Teams.Cards.TextBlock)
            concreteType = ResolveByName(typeName, typeof(CardElement));
        }

        if (concreteType is null)
        {
            throw new NotSupportedException($"Unknown CardElement type '{typeName}'.");
        }

        return (CardElement?)JsonSerializer.Deserialize(root.GetRawText(), concreteType, options);
    }

    public override void Write(Utf8JsonWriter writer, CardElement value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
    }

    private static Type? ResolveByName(string discriminator, Type baseType)
    {
        // Try to match concrete type by exact simple name, e.g. "TextBlock" -> TextBlock
        var asm = baseType.Assembly;
        var type = asm.GetTypes().FirstOrDefault(t => !t.IsAbstract && baseType.IsAssignableFrom(t) && string.Equals(t.Name, discriminator, StringComparison.Ordinal));
        if (type != null) return type;

        // Handle dot-style discriminators by removing dots and common suffix translations
        // e.g., "Input.Text" -> TextInput
        var alt = discriminator.Replace(".", string.Empty);
        var altType = asm.GetTypes().FirstOrDefault(t => !t.IsAbstract && baseType.IsAssignableFrom(t) && string.Equals(t.Name, alt, StringComparison.Ordinal));
        return altType;
    }
}

internal sealed class ActionJsonConverter : JsonConverter<Action>
{
    // Map JSON "type" to concrete Action type.
    // Extend this as you add more Action types.
    private static readonly Dictionary<string, Type> _typeMap = new(StringComparer.Ordinal)
    {
        ["Action.OpenUrl"] = typeof(OpenUrlAction),
        ["Action.Submit"] = typeof(SubmitAction),
        ["Action.ToggleVisibility"] = typeof(ToggleVisibilityAction),
        ["Action.ShowCard"] = typeof(ShowCardAction),
        ["Action.Execute"] = typeof(ExecuteAction)
    };

    public override Action? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("type", out var typeProp) || typeProp.ValueKind != JsonValueKind.String)
        {
            throw new JsonException("Action payload must contain a string 'type' property.");
        }

        var typeName = typeProp.GetString();
        if (string.IsNullOrWhiteSpace(typeName))
        {
            throw new JsonException("Action 'type' property cannot be null or empty.");
        }

        if (!_typeMap.TryGetValue(typeName, out var concreteType))
        {
            // Fallbacks by naming convention
            // "Action.OpenUrl" -> OpenUrlAction, "Action.Submit" -> SubmitAction, etc.
            var altName = typeName.StartsWith("Action.", StringComparison.Ordinal)
                ? typeName.Substring("Action.".Length) + "Action"
                : typeName;

            concreteType = ResolveByName(altName, typeof(Action));
        }

        if (concreteType is null)
        {
            throw new NotSupportedException($"Unknown Action type '{typeName}'.");
        }

        return (Action?)JsonSerializer.Deserialize(root.GetRawText(), concreteType, options);
    }

    public override void Write(Utf8JsonWriter writer, Action value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
    }

    private static Type? ResolveByName(string simpleName, Type baseType)
    {
        var asm = baseType.Assembly;
        return asm.GetTypes().FirstOrDefault(t => !t.IsAbstract && baseType.IsAssignableFrom(t) && string.Equals(t.Name, simpleName, StringComparison.Ordinal));
    }
}

internal sealed class ContainerLayoutJsonConverter : JsonConverter<ContainerLayout>
{
    // Map JSON "type" to concrete layout type.
    // Extend this as you add more layouts.
    private static readonly Dictionary<string, Type> _typeMap = new(StringComparer.Ordinal)
    {
        ["Layout.Flow"] = typeof(FlowLayout),
        ["Layout.Stack"] = typeof(StackLayout),
        ["Layout.Grid"] = typeof(AreaGridLayout)
    };

    public override ContainerLayout? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("type", out var typeProp) || typeProp.ValueKind != JsonValueKind.String)
        {
            throw new JsonException("ContainerLayout payload must contain a string 'type' property.");
        }

        var typeName = typeProp.GetString();
        if (string.IsNullOrWhiteSpace(typeName))
        {
            throw new JsonException("ContainerLayout 'type' property cannot be null or empty.");
        }

        if (!_typeMap.TryGetValue(typeName, out var concreteType))
        {
            // Fallback: try to resolve by simple name convention:
            // "Layout.Flow" -> "FlowLayout", "Layout.Stack" -> "StackLayout"
            var simple = typeName.StartsWith("Layout.", StringComparison.Ordinal)
                ? typeName["Layout.".Length..] + "Layout"
                : typeName;

            concreteType = ResolveByName(simple, typeof(ContainerLayout));
        }

        if (concreteType is null)
        {
            throw new NotSupportedException($"Unknown ContainerLayout type '{typeName}'.");
        }

        return (ContainerLayout?)JsonSerializer.Deserialize(root.GetRawText(), concreteType, options);
    }

    public override void Write(Utf8JsonWriter writer, ContainerLayout value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
    }

    private static Type? ResolveByName(string simpleName, Type baseType)
    {
        var asm = baseType.Assembly;
        return asm.GetTypes().FirstOrDefault(t => !t.IsAbstract && baseType.IsAssignableFrom(t) && string.Equals(t.Name, simpleName, StringComparison.Ordinal));
    }
}