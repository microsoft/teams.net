using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

[JsonConverter(typeof(JsonConverter<ChartColor>))]
public partial class ChartColor(string value) : StringEnum(value, caseSensitive: false)
{
    public static readonly ChartColor Good = new("good");
    public bool IsGood => Good.Equals(Value);

    public static readonly ChartColor Warning = new("warning");
    public bool IsWarning => Warning.Equals(Value);

    public static readonly ChartColor Attention = new("attention");
    public bool IsAttention => Attention.Equals(Value);

    public static readonly ChartColor Neutral = new("neutral");
    public bool IsNeutral => Neutral.Equals(Value);

    public static readonly ChartColor CategoricalRed = new("categoricalRed");
    public bool IsCategoricalRed => CategoricalRed.Equals(Value);

    public static readonly ChartColor CategoricalPurple = new("categoricalPurple");
    public bool IsCategoricalPurple => CategoricalPurple.Equals(Value);

    public static readonly ChartColor CategoricalLavender = new("categoricalLavender");
    public bool IsCategoricalLavender => CategoricalLavender.Equals(Value);

    public static readonly ChartColor CategoricalBlue = new("categoricalBlue");
    public bool IsCategoricalBlue => CategoricalBlue.Equals(Value);

    public static readonly ChartColor CategoricalLightBlue = new("categoricalLightBlue");
    public bool IsCategoricalLightBlue => CategoricalLightBlue.Equals(Value);

    public static readonly ChartColor CategoricalTeal = new("categoricalTeal");
    public bool IsCategoricalTeal => CategoricalTeal.Equals(Value);

    public static readonly ChartColor CategoricalGreen = new("categoricalGreen");
    public bool IsCategoricalGreen => CategoricalGreen.Equals(Value);

    public static readonly ChartColor CategoricalLime = new("categoricalLime");
    public bool IsCategoricalLime => CategoricalLime.Equals(Value);

    public static readonly ChartColor CategoricalMarigold = new("categoricalMarigold");
    public bool IsCategoricalMarigold => CategoricalMarigold.Equals(Value);

    public static readonly ChartColor Sequential1 = new("sequential1");
    public bool IsSequential1 => Sequential1.Equals(Value);

    public static readonly ChartColor Sequential2 = new("sequential2");
    public bool IsSequential2 => Sequential2.Equals(Value);

    public static readonly ChartColor Sequential3 = new("sequential3");
    public bool IsSequential3 => Sequential3.Equals(Value);

    public static readonly ChartColor Sequential4 = new("sequential4");
    public bool IsSequential4 => Sequential4.Equals(Value);

    public static readonly ChartColor Sequential5 = new("sequential5");
    public bool IsSequential5 => Sequential5.Equals(Value);

    public static readonly ChartColor Sequential6 = new("sequential6");
    public bool IsSequential6 => Sequential6.Equals(Value);

    public static readonly ChartColor Sequential7 = new("sequential7");
    public bool IsSequential7 => Sequential7.Equals(Value);

    public static readonly ChartColor Sequential8 = new("sequential8");
    public bool IsSequential8 => Sequential8.Equals(Value);

    public static readonly ChartColor DivergingBlue = new("divergingBlue");
    public bool IsDivergingBlue => DivergingBlue.Equals(Value);

    public static readonly ChartColor DivergingLightBlue = new("divergingLightBlue");
    public bool IsDivergingLightBlue => DivergingLightBlue.Equals(Value);

    public static readonly ChartColor DivergingCyan = new("divergingCyan");
    public bool IsDivergingCyan => DivergingCyan.Equals(Value);

    public static readonly ChartColor DivergingTeal = new("divergingTeal");
    public bool IsDivergingTeal => DivergingTeal.Equals(Value);

    public static readonly ChartColor DivergingYellow = new("divergingYellow");
    public bool IsDivergingYellow => DivergingYellow.Equals(Value);

    public static readonly ChartColor DivergingPeach = new("divergingPeach");
    public bool IsDivergingPeach => DivergingPeach.Equals(Value);

    public static readonly ChartColor DivergingLightRed = new("divergingLightRed");
    public bool IsDivergingLightRed => DivergingLightRed.Equals(Value);

    public static readonly ChartColor DivergingRed = new("divergingRed");
    public bool IsDivergingRed => DivergingRed.Equals(Value);

    public static readonly ChartColor DivergingMaroon = new("divergingMaroon");
    public bool IsDivergingMaroon => DivergingMaroon.Equals(Value);

    public static readonly ChartColor DivergingGray = new("divergingGray");
    public bool IsDivergingGray => DivergingGray.Equals(Value);
}