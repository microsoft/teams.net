using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public partial class CardType : StringEnum
{
    public static readonly CardType Container = new("Container");
    public bool IsContainer => Container.Equals(Value);
}

/// <summary>
/// The style of the container. Container styles control the colors of the background, border and text inside the container, in such a way that contrast requirements are always met.
/// </summary>
[JsonConverter(typeof(JsonConverter<ContainerStyle>))]
public partial class ContainerStyle(string value) : StringEnum(value, caseSensitive: false)
{
    public static readonly ContainerStyle Default = new("default");
    public bool IsDefault => Default.Equals(Value);

    public static readonly ContainerStyle Emphasis = new("emphasis");
    public bool IsEmphasis => Emphasis.Equals(Value);

    public static readonly ContainerStyle Good = new("good");
    public bool IsGood => Good.Equals(Value);

    public static readonly ContainerStyle Attention = new("attention");
    public bool IsAttention => Attention.Equals(Value);

    public static readonly ContainerStyle Warning = new("warning");
    public bool IsWarning => Warning.Equals(Value);

    public static readonly ContainerStyle Accent = new("accent");
    public bool IsAccent => Accent.Equals(Value);
}

/// <summary>
/// Containers group items together.
/// </summary>
public class Container(params Element[] items) : ContainerElement(CardType.Container)
{
    /// <summary>
    /// The card elements to render inside the `Container`.
    /// </summary>
    [JsonPropertyName("items")]
    [JsonPropertyOrder(19)]
    public IList<Element> Items { get; set; } = items;

    /// <summary>
    /// Style hint for `Container`.
    /// </summary>
    [JsonPropertyName("style")]
    [JsonPropertyOrder(20)]
    public ContainerStyle? Style { get; set; }

    /// <summary>
    /// Defines how the content should be aligned vertically within the container. When not specified, the value of verticalContentAlignment is inherited from the parent container. If no parent container has verticalContentAlignment set, it defaults to Top.
    /// </summary>
    [JsonPropertyName("verticalContentAlignment")]
    [JsonPropertyOrder(21)]
    public VerticalAlignment? VerticalContentAlignment;

    /// <summary>
    /// Specifies the minimum height of the container in pixels, like `\"80px\"`.
    /// </summary>
    [JsonPropertyName("minHeight")]
    [JsonPropertyOrder(22)]
    public string? MinHeight { get; set; }

    public Container WithStyle(ContainerStyle value)
    {
        Style = value;
        return this;
    }

    public Container WithVerticalAlignment(VerticalAlignment value)
    {
        VerticalContentAlignment = value;
        return this;
    }

    public Container WithMinHeight(string value)
    {
        MinHeight = value;
        return this;
    }

    public Container AddCards(params Element[] value)
    {
        foreach (var card in value)
        {
            Items.Add(card);
        }

        return this;
    }
}