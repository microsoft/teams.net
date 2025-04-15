using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Cards;

public abstract class Element(CardType type)
{
    /// <summary>
    /// A unique identifier associated with the item
    /// </summary>
    [JsonPropertyName("id")]
    [JsonPropertyOrder(0)]
    public string? Id { get; set; }

    /// <summary>
    /// the element type
    /// </summary>
    [JsonPropertyName("type")]
    [JsonPropertyOrder(1)]
    public CardType Type { get; set; } = type;

    /// <summary>
    /// If false, this item will be removed from the visual tree.
    /// </summary>
    [JsonPropertyName("isVisible")]
    [JsonPropertyOrder(2)]
    public bool? IsVisible { get; set; }

    /// <summary>
    /// A series of key/value pairs indicating features that the item requires with corresponding minimum version. When a feature is missing or of insufficient version, fallback is triggered.
    /// </summary>
    [JsonPropertyName("requires")]
    [JsonPropertyOrder(3)]
    public IDictionary<string, string>? Requires { get; set; }

    /// <summary>
    /// Specifies the height of the element.
    /// </summary>
    [JsonPropertyName("height")]
    [JsonPropertyOrder(4)]
    public Height? Height { get; set; }

    /// <summary>
    /// When `true`, draw a separating line at the top of the element.
    /// </summary>
    [JsonPropertyName("separator")]
    [JsonPropertyOrder(5)]
    public bool? Separator { get; set; }

    /// <summary>
    /// Controls the amount of spacing between this element and the preceding element.
    /// </summary>
    [JsonPropertyName("spacing")]
    [JsonPropertyOrder(6)]
    public Spacing? Spacing { get; set; }

    /// <summary>
    /// the area of a `Layout.AreaGrid` layout in which an element should be displayed.
    /// </summary>
    [JsonPropertyName("grid.area")]
    [JsonPropertyOrder(7)]
    public string? GridArea { get; set; }

    /// <summary>
    /// controls how the element should be horizontally aligned.
    /// </summary>
    [JsonPropertyName("horizontalAlignment")]
    [JsonPropertyOrder(8)]
    public HorizontalAlignment? HorizontalAlignment { get; set; }

    /// <summary>
    /// Controls for which card width the element should be displayed. If targetWidth isn't specified, the element is rendered at all card widths. Using targetWidth makes it possible to author responsive cards that adapt their layout to the available horizontal space.
    /// </summary>
    [JsonPropertyName("targetWidth")]
    [JsonPropertyOrder(9)]
    public TargetWidth? TargetWidth { get; set; }

    /// <summary>
    /// The locale associated with the element.
    /// </summary>
    [JsonPropertyName("lang")]
    [JsonPropertyOrder(10)]
    public string? Lang { get; set; }

    /// <summary>
    /// Describes what to do when an unknown item is encountered or the requires of this or any children can't be met.
    /// </summary>
    [JsonPropertyName("fallback")]
    [JsonPropertyOrder(11)]
    public Element? Fallback { get; set; }

    public Element WithId(string value)
    {
        Id = value;
        return this;
    }

    public Element WithIsVisible(bool value)
    {
        IsVisible = value;
        return this;
    }

    public Element WithRequires(IDictionary<string, string> value)
    {
        Requires = value;
        return this;
    }

    public Element WithRequire(string key, string value)
    {
        Requires ??= new Dictionary<string, string>();
        Requires.Add(key, value);
        return this;
    }

    public Element WithHeight(Height value)
    {
        Height = value;
        return this;
    }

    public Element WithSeparator(bool value = true)
    {
        Separator = value;
        return this;
    }

    public Element WithSpacing(Spacing value)
    {
        Spacing = value;
        return this;
    }

    public Element WithGridArea(string value)
    {
        GridArea = value;
        return this;
    }

    public Element WithHorizontalAlignment(HorizontalAlignment value)
    {
        HorizontalAlignment = value;
        return this;
    }

    public Element WithTargetWidth(TargetWidth value)
    {
        TargetWidth = value;
        return this;
    }

    public Element WithLang(string value)
    {
        Lang = value;
        return this;
    }

    public Element WithFallback(Element value)
    {
        Fallback = value;
        return this;
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }
}