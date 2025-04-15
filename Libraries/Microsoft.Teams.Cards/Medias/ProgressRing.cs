using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public partial class CardType : StringEnum
{
    public static readonly CardType ProgressRing = new("ProgressRing");
    public bool IsProgressRing => ProgressRing.Equals(Value);
}

/// <summary>
/// Controls the relative position of the label to the progress ring.
/// </summary>
[JsonConverter(typeof(JsonConverter<ProgressRingLabelPosition>))]
public partial class ProgressRingLabelPosition(string value) : StringEnum(value)
{
    public static readonly ProgressRingLabelPosition Before = new("before");
    public bool IsBefore => Before.Equals(Value);

    public static readonly ProgressRingLabelPosition After = new("after");
    public bool IsAfter => After.Equals(Value);

    public static readonly ProgressRingLabelPosition Above = new("above");
    public bool IsAbove => Above.Equals(Value);

    public static readonly ProgressRingLabelPosition Below = new("below");
    public bool IsBelow => Below.Equals(Value);
}

/// <summary>
/// The size of the progress ring.
/// </summary>
[JsonConverter(typeof(JsonConverter<ProgressRingSize>))]
public partial class ProgressRingSize(string value) : StringEnum(value)
{
    public static readonly ProgressRingSize Tiny = new("tiny");
    public bool IsTiny => Tiny.Equals(Value);

    public static readonly ProgressRingSize Small = new("small");
    public bool IsSmall => Small.Equals(Value);

    public static readonly ProgressRingSize Medium = new("medium");
    public bool IsMedium => Medium.Equals(Value);

    public static readonly ProgressRingSize Large = new("large");
    public bool IsLarge => Large.Equals(Value);
}

/// <summary>
/// A spinning ring element, to indicate progress.
/// </summary>
public class ProgressRing() : Element(CardType.ProgressRing)
{
    /// <summary>
    /// The label of the progress ring.
    /// </summary>
    [JsonPropertyName("label")]
    [JsonPropertyOrder(12)]
    public string? Label { get; set; }

    /// <summary>
    /// Controls the relative position of the label to the progress ring.
    /// </summary>
    [JsonPropertyName("labelPosition")]
    [JsonPropertyOrder(13)]
    public ProgressRingLabelPosition? LabelPosition { get; set; }

    /// <summary>
    /// The size of the progress ring.
    /// </summary>
    [JsonPropertyName("size")]
    [JsonPropertyOrder(14)]
    public ProgressRingSize? Size { get; set; }

    public ProgressRing WithLabel(string value, ProgressRingLabelPosition? position)
    {
        Label = value;
        LabelPosition ??= position;
        return this;
    }

    public ProgressRing WithLabelPosition(ProgressRingLabelPosition value)
    {
        LabelPosition = value;
        return this;
    }

    public ProgressRing WithSize(ProgressRingSize value)
    {
        Size = value;
        return this;
    }
}