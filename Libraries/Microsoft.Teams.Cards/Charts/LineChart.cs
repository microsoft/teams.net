using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Cards;

public partial class CardType : StringEnum
{
    public static readonly CardType LineChart = new("Chart.Line");
    public bool IsLineChart => LineChart.Equals(Value);
}

public class LineChart(params LineChartData[] data) : Element(CardType.LineChart)
{
    /// <summary>
    /// the title of the chart.
    /// </summary>
    [JsonPropertyName("title")]
    [JsonPropertyOrder(12)]
    public string? Title { get; set; }

    /// <summary>
    /// the color to use for all data points.
    /// </summary>
    [JsonPropertyName("color")]
    [JsonPropertyOrder(13)]
    public ChartColor? Color { get; set; }

    /// <summary>
    /// the name of the set of colors to use.
    /// </summary>
    [JsonPropertyName("colorSet")]
    [JsonPropertyOrder(14)]
    public string? ColorSet { get; set; }

    /// <summary>
    /// the data to display in the chart.
    /// </summary>
    [JsonPropertyName("data")]
    [JsonPropertyOrder(15)]
    public IList<LineChartData> Data { get; set; } = data;

    public LineChart WithTitle(string value)
    {
        Title = value;
        return this;
    }

    public LineChart WithColor(ChartColor value)
    {
        Color = value;
        return this;
    }

    public LineChart WithColorSet(string value)
    {
        ColorSet = value;
        return this;
    }

    public LineChart AddData(params LineChartData[] value)
    {
        foreach (var datapoint in value)
        {
            Data.Add(datapoint);
        }

        return this;
    }
}

public class LineChartData(params LineChartDataPoint[] values)
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
    /// the data points in the series.
    /// </summary>
    [JsonPropertyName("values")]
    [JsonPropertyOrder(2)]
    public IList<LineChartDataPoint> Values { get; set; } = values;
}

public class LineChartDataPoint
{
    /// <summary>
    /// the x axis value of the data point.
    /// </summary>
    [JsonPropertyName("x")]
    [JsonPropertyOrder(0)]
    public IUnion<string, double> X { get; set; }

    /// <summary>
    /// the y axis value of the data point.
    /// </summary>
    [JsonPropertyName("y")]
    [JsonPropertyOrder(1)]
    public double Y { get; set; }

    public LineChartDataPoint(string x, double y)
    {
        X = new Union<string, double>(x);
        Y = y;
    }

    public LineChartDataPoint(double x, double y)
    {
        X = new Union<string, double>(x);
        Y = y;
    }
}