using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public partial class CardType : StringEnum
{
    public static readonly CardType DonutChart = new("Chart.Donut");
    public bool IsDonutChart => DonutChart.Equals(Value);
}

public class DonutChart(params DonutChartData[] data) : Element(CardType.DonutChart)
{
    /// <summary>
    /// the title of the chart.
    /// </summary>
    [JsonPropertyName("title")]
    [JsonPropertyOrder(12)]
    public string? Title { get; set; }

    /// <summary>
    /// the name of the set of colors to use.
    /// </summary>
    [JsonPropertyName("colorSet")]
    [JsonPropertyOrder(13)]
    public string? ColorSet { get; set; }

    /// <summary>
    /// the data to display in the chart.
    /// </summary>
    [JsonPropertyName("data")]
    [JsonPropertyOrder(14)]
    public IList<DonutChartData> Data { get; set; } = data;

    public DonutChart WithTitle(string value)
    {
        Title = value;
        return this;
    }

    public DonutChart WithColorSet(string value)
    {
        ColorSet = value;
        return this;
    }

    public DonutChart AddData(params DonutChartData[] value)
    {
        foreach (var datapoint in value)
        {
            Data.Add(datapoint);
        }

        return this;
    }
}

public class DonutChartData
{
    /// <summary>
    /// the color to use for the data point.
    /// </summary>
    [JsonPropertyName("color")]
    [JsonPropertyOrder(0)]
    public ChartColor? Color { get; set; }

    /// <summary>
    /// the legend of the chart.
    /// </summary>
    [JsonPropertyName("legend")]
    [JsonPropertyOrder(1)]
    public string? Legend { get; set; }

    /// <summary>
    /// the value associated with the data point.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonPropertyOrder(2)]
    public required int Value { get; set; }
}