using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public partial class CardType : StringEnum
{
    public static readonly CardType ChoiceSetInput = new("Input.ChoiceSet");
    public bool IsChoiceSetInput => ChoiceSetInput.Equals(Value);
}

/// <summary>
/// Style hint for text input.
/// </summary>
[JsonConverter(typeof(JsonConverter<ChoiceInputStyle>))]
public partial class ChoiceInputStyle(string value) : StringEnum(value, caseSensitive: false)
{
    public static readonly ChoiceInputStyle Compact = new("compact");
    public bool IsCompact => Compact.Equals(Value);

    public static readonly ChoiceInputStyle Expanded = new("expanded");
    public bool IsExpanded => Expanded.Equals(Value);

    public static readonly ChoiceInputStyle Filtered = new("filtered");
    public bool IsFiltered => Filtered.Equals(Value);
}

/// <summary>
/// Allows a user to input a Choice.
/// </summary>
public class ChoiceSetInput(params Choice[] choices) : InputElement(CardType.ChoiceSetInput)
{
    /// <summary>
    /// Choice options.
    /// </summary>
    [JsonPropertyName("choices")]
    [JsonPropertyOrder(18)]
    public IList<Choice> Choices { get; set; } = choices;

    /// <summary>
    /// Allows dynamic fetching of choices from the bot to be displayed as suggestions in the dropdown when the user types in the input field.
    /// </summary>
    [JsonPropertyName("choices.data")]
    [JsonPropertyOrder(19)]
    public ChoiceDataQuery? Data { get; set; }

    /// <summary>
    /// Allow multiple choices to be selected.
    /// </summary>
    [JsonPropertyName("isMultiSelect")]
    [JsonPropertyOrder(20)]
    public bool? IsMultiSelect { get; set; }

    /// <summary>
    /// the style of the choice input
    /// </summary>
    [JsonPropertyName("style")]
    [JsonPropertyOrder(21)]
    public ChoiceInputStyle? Style { get; set; }

    /// <summary>
    /// The initial choice (or set of choices) that should be selected. For multi-select, specify a comma-separated string of values.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonPropertyOrder(22)]
    public string? Value { get; set; }

    /// <summary>
    /// Description of the input desired. Only visible when no selection has been made, the `style` is `compact` and `isMultiSelect` is `false`
    /// </summary>
    [JsonPropertyName("placeholder")]
    [JsonPropertyOrder(23)]
    public string? Placeholder { get; set; }

    /// <summary>
    /// If `true`, allow text to wrap. Otherwise, text is clipped.
    /// </summary>
    [JsonPropertyName("wrap")]
    [JsonPropertyOrder(24)]
    public bool? Wrap { get; set; }

    public ChoiceSetInput WithData(ChoiceDataQuery value)
    {
        Data = value;
        return this;
    }

    public ChoiceSetInput WithMultiSelect(bool value = true)
    {
        IsMultiSelect = value;
        return this;
    }

    public ChoiceSetInput WithStyle(ChoiceInputStyle value)
    {
        Style = value;
        return this;
    }

    public ChoiceSetInput WithValue(string value)
    {
        Value = value;
        return this;
    }

    public ChoiceSetInput WithPlaceholder(string value)
    {
        Placeholder = value;
        return this;
    }

    public ChoiceSetInput WithWrap(bool value = true)
    {
        Wrap = value;
        return this;
    }

    public ChoiceSetInput AddChoices(params Choice[] value)
    {
        foreach (var choice in value)
        {
            Choices.Add(choice);
        }

        return this;
    }
}